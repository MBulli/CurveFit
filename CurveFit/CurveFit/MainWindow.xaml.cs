using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CurveFit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            inkCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(InkCanvas_OnMouseDown), true);
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.Strokes.Count > 0)
            {
                Stroke lastStroke = inkCanvas.Strokes.Last();

                StringBuilder sb = new StringBuilder();

                foreach (var p in lastStroke.StylusPoints)
                {
                    sb.AppendLine($"{p.X},{inkCanvas.ActualHeight - p.Y}");
                }

                File.WriteAllText("test.csv", sb.ToString());
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            inkCanvas.Strokes.Clear();
        }

        private IEnumerable<double> XValues()
        {
            return inkCanvas.Strokes.Last().StylusPoints.Select(p => p.X);
        }

        private IEnumerable<double> YValues()
        {
            return inkCanvas.Strokes.Last().StylusPoints.Select(p => p.Y);
        }

        private Vector<double> Sin(Vector<double> v)
            => v.Map(Math.Sin);

        private IEnumerable<double> Range(double start, double step, double end)
        {
            for (double i = start; i < end; i += step)
            {
                yield return i;
            }
        }
        
        private Matrix<double> SineFunction(Vector<double> input, Vector<double> freqRange)
        {
            Matrix<double> result = Matrix<double>.Build.Dense(input.Count, freqRange.Count);
            for (int i = 0; i < freqRange.Count; i++)
            {
                result.SetColumn(i, Sin(freqRange[i] * input));
            }
            return result;
        }

        private Matrix<double> PolynomialFunction(Vector<double> input, int degree)
        {
            Debug.Assert(degree > 0);
            Matrix<double> result = Matrix<double>.Build.Dense(input.Count, degree);
            for (int i = 0; i < degree; i++)
            {
                result.SetColumn(i, input.PointwisePower(i+1));
            }
            return result;
        }

        private Matrix<double> PrepareMatrix(Vector<double> input)
        {
            int resultColCount = 1;

            Matrix<double> sineData = null;
            if (sinesEnabled)
            {
                sineData = SineFunction(input, Vector<double>.Build.DenseOfEnumerable(Range(minFreq, stepFreq, maxFreq)) * (Math.PI / 1000)); // inkCanvas.ActualWidth ca 1000
                resultColCount += sineData.ColumnCount;
            }

            Matrix<double> polynomialData = null;
            if (polinomEnabled)
            {
                polynomialData = PolynomialFunction(input, polinomDegree);
                resultColCount += polynomialData.ColumnCount;
            }

            var result = Matrix<double>.Build.Dense(input.Count, resultColCount);
            result.SetColumn(0, Vector<double>.Build.Dense(input.Count, 1));
            int colIndex = 1;

            if (sinesEnabled)
            {
                result.SetSubMatrix(0, colIndex, sineData);
                colIndex += sineData.ColumnCount;
            }

            if (polinomEnabled)
            {
                result.SetSubMatrix(0, colIndex, polynomialData);
                //colIndex += polynomialData.ColumnCount;
            }
            return result;
        }

        private bool polinomEnabled = false;
        private int polinomDegree = 4;

        private bool sinesEnabled = true;
        private double minFreq = 0.25;
        private double stepFreq = 0.25;
        private double maxFreq = 2.0;

        private bool plotForAllX = true;
        private void FitCurve()
        {
            var x = Vector<double>.Build.DenseOfEnumerable(XValues());
            var y = Vector<double>.Build.DenseOfEnumerable(YValues());

            var A = PrepareMatrix(x);

            var parameter = A.PseudoInverse() * y;

            Vector<double> plotX;
            Vector<double> plotY;
            if (plotForAllX)
            {
                plotX = Vector<double>.Build.DenseOfEnumerable(Range(0, 1, inkCanvas.ActualWidth));
                plotY = PrepareMatrix(plotX)*parameter;
            }
            else
            {
                plotX = x;
                plotY = A*parameter;
            }

            var result = new StylusPointCollection();
            for (int i = 0; i < plotX.Count; i++)
            {
                result.Add(new StylusPoint(plotX[i], plotY[i]));
            }
            inkCanvas.Strokes.Add(new Stroke(result, new DrawingAttributes { Color = Colors.Red }));
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            FitCurve();
        }

        private void inkCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            inkCanvas.Strokes.Clear();
        }

        private void inkCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            FitCurve();
        }

        private void InkCanvas_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            FitCurve();
        }

        private void InkCanvas_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            inkCanvas.Strokes.Clear();
        }
    }
}

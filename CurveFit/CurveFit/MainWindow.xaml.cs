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
        private bool PolynomEnabled => UsePolynomCheckBox.IsChecked ?? false;
        private int PolynomDegree => (int) DegreeSlider.Value;

        private bool SinesEnabled => UseSinesCheckBox.IsChecked ?? false;
        private double MinFreq => MinFreqSlider.Value;
        private double StepFreq => StepFreqSlider.Value;
        private double MaxFreq => MaxFreqSlider.Value;

        private bool plotForAllX = true;

        public MainWindow()
        {
            InitializeComponent();

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            InkCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(InkCanvas_OnMouseDown), true);
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (InkCanvas.Strokes.Count > 0)
            {
                Stroke lastStroke = InkCanvas.Strokes.Last();

                StringBuilder sb = new StringBuilder();

                foreach (var p in lastStroke.StylusPoints)
                {
                    sb.AppendLine($"{p.X},{InkCanvas.ActualHeight - p.Y}");
                }

                File.WriteAllText("test.csv", sb.ToString());
            }
        }

        private void DegreeSlider_OnClick(object sender, RoutedEventArgs e)
        {
            InkCanvas.Strokes.Clear();
        }

        private IEnumerable<double> XValues()
        {
            return InkCanvas.Strokes.Last().StylusPoints.Select(p => p.X);
        }

        private IEnumerable<double> YValues()
        {
            return InkCanvas.Strokes.Last().StylusPoints.Select(p => p.Y);
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
            if (SinesEnabled)
            {
                sineData = SineFunction(input, Vector<double>.Build.DenseOfEnumerable(Range(MinFreq, StepFreq, MaxFreq)) * (Math.PI / 1000)); // inkCanvas.ActualWidth ca 1000
                resultColCount += sineData.ColumnCount;
            }

            Matrix<double> polynomialData = null;
            if (PolynomEnabled)
            {
                polynomialData = PolynomialFunction(input, PolynomDegree);
                resultColCount += polynomialData.ColumnCount;
            }

            var result = Matrix<double>.Build.Dense(input.Count, resultColCount);
            result.SetColumn(0, Vector<double>.Build.Dense(input.Count, 1));
            int colIndex = 1;

            if (SinesEnabled)
            {
                result.SetSubMatrix(0, colIndex, sineData);
                colIndex += sineData.ColumnCount;
            }

            if (PolynomEnabled)
            {
                result.SetSubMatrix(0, colIndex, polynomialData);
                //colIndex += polynomialData.ColumnCount;
            }
            return result;
        }

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
                plotX = Vector<double>.Build.DenseOfEnumerable(Range(0, 1, InkCanvas.ActualWidth));
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
            InkCanvas.Strokes.Add(new Stroke(result, new DrawingAttributes { Color = Colors.Red }));
        }

        private void FitButton_OnClick(object sender, RoutedEventArgs e)
        {
            FitCurve();
        }

        private void inkCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            InkCanvas.Strokes.Clear();
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
            InkCanvas.Strokes.Clear();
        }
    }
}

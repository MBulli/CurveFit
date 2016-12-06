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

        private bool PlotForAllX => PlotAllXCheckBox.IsChecked ?? false;

        private readonly List<Func<Vector<double>, Matrix<double>>> _availableFunctions;

        public MainWindow()
        {
            InitializeComponent();

            InkCanvas.AddHandler(InkCanvas.MouseDownEvent, new MouseButtonEventHandler(InkCanvas_OnMouseDown), true);

            _availableFunctions = new List<Func<Vector<double>, Matrix<double>>>
            {
                BaselineFunction,
                SineFunction,
                PolynomialFunction
            };
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

        private Matrix<double> BaselineFunction(Vector<double> input)
        {
            return Vector<double>.Build.Dense(input.Count, 1).ToColumnMatrix();
        }

        private Matrix<double> SineFunction(Vector<double> input)
        {
            if (!SinesEnabled)
                return null;

            var freqRange = Vector<double>.Build.DenseOfEnumerable(Range(MinFreq, StepFreq, MaxFreq))*(Math.PI/1000);
            Matrix<double> result = Matrix<double>.Build.Dense(input.Count, freqRange.Count);
            for (int i = 0; i < freqRange.Count; i++)
            {
                result.SetColumn(i, Sin(freqRange[i] * input));
            }
            return result;
        }

        private Matrix<double> PolynomialFunction(Vector<double> input)
        {
            if (!PolynomEnabled)
                return null;

            Debug.Assert(PolynomDegree > 0);
            Matrix<double> result = Matrix<double>.Build.Dense(input.Count, PolynomDegree);
            for (int i = 0; i < PolynomDegree; i++)
            {
                result.SetColumn(i, input.PointwisePower(i+1));
            }
            return result;
        }

        private Matrix<double> PrepareMatrix(Vector<double> input)
        {
            var matrices = _availableFunctions.Select(func => func(input)).Where(x => x != null);
            return Matrix<double>.Build.DenseOfColumnVectors(matrices.SelectMany(matrix => matrix.EnumerateColumns()));
        }

        private void FitCurve()
        {
            var x = Vector<double>.Build.DenseOfEnumerable(XValues());
            var y = Vector<double>.Build.DenseOfEnumerable(YValues());

            var A = PrepareMatrix(x);

            var parameter = A.PseudoInverse() * y;

            Vector<double> plotX;
            Vector<double> plotY;
            if (PlotForAllX)
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

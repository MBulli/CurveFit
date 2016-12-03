using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
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


        private Matrix<double> PseudoInverse(Matrix<double> m)
        {
            double eps = 0;

            var svd = m.Svd();
            var V = svd.VT.Transpose();
            var U = svd.U;
            var S = svd.W;

            var Sigma = Matrix<double>.Build.DenseOfDiagonalVector(rows: V.ColumnCount,
                                                                   columns: U.RowCount,
                                                                   diagonal: svd.S.PointwisePower(-1));

            return V * Sigma * U.Transpose();
        }

        private void FitCurve()
        {
            var x = Vector<double>.Build.DenseOfEnumerable(XValues());
            var y = Vector<double>.Build.DenseOfEnumerable(YValues());

            double k = 1 / 160.0;

            var kv = Vector<double>.Build.DenseOfEnumerable(Range(0.5, 0.1, 2.0));
            kv *= k;

            var A = Matrix<double>.Build.Dense(x.Count, kv.Count + 1);

            for (int i = 0; i < A.RowCount; i++)
            {
                var row = Vector<double>.Build.Dense(kv.Count + 1);
                row[0] = 1;

                var factors = Sin(kv * x[i]);
                factors.CopySubVectorTo(row, 0, 1, kv.Count);

                A.SetRow(i, row);
            }

            var parameter = PseudoInverse(A) * y;

            var result = new StylusPointCollection();
            for (int i = 0; i < inkCanvas.ActualWidth; i++)
            {
                var row = Vector<double>.Build.Dense(kv.Count + 1);
                row[0] = 1;
                var factors = Sin(kv * i);
                factors.CopySubVectorTo(row, 0, 1, kv.Count);
                var j = parameter.PointwiseMultiply(row).Sum();
                result.Add(new StylusPoint(i, j));
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
    }
}

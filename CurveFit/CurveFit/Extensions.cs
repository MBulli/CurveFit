using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace CurveFit
{
    static class Extensions
    {
        public static Matrix<double> PseudoInverse(this Matrix<double> m, double eps = 1e-5)
        {
            var svd = m.Svd();
            var V = svd.VT.Transpose();

            var Sigma = Matrix<double>.Build.DenseOfDiagonalVector(rows: V.ColumnCount,
                                                                   columns: svd.U.RowCount,
                                                                   diagonal: svd.S.Map(s => (s >= eps) ? (1.0 / s) : 0));

            return V * Sigma * svd.U.Transpose();
        }
    }
}

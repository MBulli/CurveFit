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
        public static Matrix<double> PseudoInverse(this Matrix<double> m)
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
    }
}

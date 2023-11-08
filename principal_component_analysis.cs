// https://jamesmccaffrey.wordpress.com/2023/11/07/principal-component-analysis-pca-from-scratch-using-csharp/

using System;
using System.IO;

namespace PrincipalComponent
{
    internal class PrincipalComponentProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\nBegin PCA from scratch" +
              " using C# ");

            Console.WriteLine("\nLoading Iris test data ");
            string testFile =
              "..\\..\\..\\Data\\iris_train.txt";
            double[][] testX = MatLoad(testFile,
              new int[] { 0, 1, 2, 3 }, ',', "#");
            Console.WriteLine("Done ");

            Console.WriteLine("\nStandardizing (z-score," +
              " biased) ");
            double[] means;
            double[] stds;
            double[][] stdX = MatStandardize(testX,
              out means, out stds);

            //VecShow(means, 4, 9);
            //VecShow(stds, 4, 9);

            Console.WriteLine("\nFirst three data items ");
            for (int i = 0; i < 3; ++i)
                VecShow(stdX[i], 4, 9);
            Console.WriteLine(". . . ");

            Console.WriteLine("\nComputing covariance matrix: ");
            double[][] covarMat = CovarMatrix(stdX, false);
            MatShow(covarMat, 4, 9);

            Console.WriteLine("\nComputing and sorting" +
              " eigenvalues and eigenvectors: ");

            double[] eigenVals;
            double[][] eigenVecs;
            Eigen(covarMat, out eigenVals, out eigenVecs);

            // sort eigenvals from large to smallest
            int[] idxs = ArgSort(eigenVals);  // save to sort evecs
            Array.Reverse(idxs);
            //VecShow(idxs, 3);

            Array.Sort(eigenVals);
            Array.Reverse(eigenVals);

            Console.WriteLine("\nEigenvalues (sorted): ");
            VecShow(eigenVals, 4, 9);

            eigenVecs = MatExtractCols(eigenVecs, idxs);  // sort 
            eigenVecs = MatTranspose(eigenVecs);  // as rows

            Console.WriteLine("\nEigenvectors (sorted, rows): ");
            MatShow(eigenVecs, 4, 9);

            Console.WriteLine("\nComputing variance" +
              " explained: ");
            double sum = 0.0;
            for (int i = 0; i < eigenVals.Length; ++i)
                sum += eigenVals[i];
            for (int i = 0; i < eigenVals.Length; ++i)
            {
                double pctExplained = eigenVals[i] / sum;
                Console.WriteLine(pctExplained.ToString("F4"));
            }

            Console.WriteLine("\nComputing transformed" +
              " data (2 components): ");
            double[][] transformed =
              MatProduct(stdX, MatTranspose(eigenVecs));  // all 
            double[][] reduced = MatExtractCols(transformed,
              new int[] { 0, 1 });  // first 2 
            float[] reducedF = new float[reduced.Length * reduced[0].Length];
            for (int i = 0, c = 0; i < reduced.Length; i++)
            {
                for (int j = 0; j < reduced[i].Length; j++)
                {
                    reducedF[c++] = (float)reduced[i][j];
                }
            }
            for (int i = 0; i < 3; ++i)
                VecShow(reduced[i], 4, 9);
            Console.WriteLine(". . . ");

            Console.WriteLine("\nEnd demo ");
            Console.ReadLine();
        } // Main

        // ------------------------------------------------------

        static double[][] MatStandardize(double[][] data,
          out double[] means, out double[] stds)
        {
            // scikit style z-score biased normalization
            int rows = data.Length;
            int cols = data[0].Length;
            double[][] result = MatCreate(rows, cols);

            // compute means
            double[] mns = new double[cols];
            for (int j = 0; j < cols; ++j)
            {
                double sum = 0.0;
                for (int i = 0; i < rows; ++i)
                    sum += data[i][j];
                mns[j] = sum / rows;
            } // j

            // compute std devs
            double[] sds = new double[cols];
            for (int j = 0; j < cols; ++j)
            {
                double sum = 0.0;
                for (int i = 0; i < rows; ++i)
                    sum += (data[i][j] - mns[j]) *
                      (data[i][j] - mns[j]);
                sds[j] = Math.Sqrt(sum / rows);  // biased version
            } // j

            // normalize
            for (int j = 0; j < cols; ++j)
            {
                for (int i = 0; i < rows; ++i)
                    result[i][j] = (data[i][j] - mns[j]) / sds[j];
            } // j

            means = mns;
            stds = sds;

            return result;
        }

        // -------------------------------------------------------

        static int[] ArgSort(double[] vec)
        {
            int n = vec.Length;
            int[] idxs = new int[n];
            for (int i = 0; i < n; ++i)
                idxs[i] = i;
            Array.Sort(vec, idxs);  // sort idxs based on vec vals
            return idxs;
        }

        // ------------------------------------------------------

        static double[][] MatExtractCols(double[][] mat,
          int[] cols)
        {
            int srcRows = mat.Length;
            int srcCols = mat[0].Length;
            int tgtCols = cols.Length;

            double[][] result = MatCreate(srcRows, tgtCols);
            for (int i = 0; i < srcRows; ++i)
            {
                for (int j = 0; j < tgtCols; ++j)
                {
                    int c = cols[j];
                    result[i][j] = mat[i][c];
                }
            }
            return result;
        }

        // ------------------------------------------------------

        static double Covariance(double[] v1, double[] v2)
        {
            // compute means of v1 and v2
            int n = v1.Length;

            double sum1 = 0.0;
            for (int i = 0; i < n; ++i)
                sum1 += v1[i];
            double mean1 = sum1 / n;

            double sum2 = 0.0;
            for (int i = 0; i < n; ++i)
                sum2 += v2[i];
            double mean2 = sum2 / n;

            // compute covariance
            double sum = 0.0;
            for (int i = 0; i < n; ++i)
                sum += (v1[i] - mean1) * (v2[i] - mean2);
            double result = sum / (n - 1);

            return result;
        }

        // ------------------------------------------------------

        static double[][] CovarMatrix(double[][] data,
          bool rowVar)
        {
            // rowVar == true means each row is a variable
            // if false, each column is a variable

            double[][] source;
            if (rowVar == true)
                source = data;  // by ref
            else
                source = MatTranspose(data);

            int srcRows = source.Length;  // num features
            int srcCols = source[0].Length;  // not used

            double[][] result = MatCreate(srcRows, srcRows);

            for (int i = 0; i < result.Length; ++i)
            {
                for (int j = 0; j <= i; ++j)
                {
                    result[i][j] = Covariance(source[i], source[j]);
                    result[j][i] = result[i][j];
                }
            }

            return result;
        }

        // ------------------------------------------------------

        static int NumNonCommentLines(string fn,
          string comment)
        {
            int ct = 0;
            string line = "";
            FileStream ifs = new FileStream(fn, FileMode.Open);
            StreamReader sr = new StreamReader(ifs);
            while ((line = sr.ReadLine()) != null)
                if (line.StartsWith(comment) == false)
                    ++ct;
            sr.Close(); ifs.Close();
            return ct;
        }

        // ------------------------------------------------------

        static double[][] MatLoad(string fn, int[] usecols,
          char sep, string comment)
        {
            // count number of non-comment lines
            int nRows = NumNonCommentLines(fn, comment);

            int nCols = usecols.Length;
            double[][] result = MatCreate(nRows, nCols);
            string line = "";
            string[] tokens = null;
            FileStream ifs = new FileStream(fn, FileMode.Open);
            StreamReader sr = new StreamReader(ifs);

            int i = 0;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith(comment) == true)
                    continue;
                tokens = line.Split(sep);
                for (int j = 0; j < nCols; ++j)
                {
                    int k = usecols[j];  // into tokens
                    result[i][j] = double.Parse(tokens[k]);
                }
                ++i;
            }
            sr.Close(); ifs.Close();
            return result;
        }

        // ------------------------------------------------------

        static void MatDecomposeQR(double[][] mat,
          out double[][] q, out double[][] r,
          bool standardize)
        {
            // QR decomposition, Householder algorithm.
            // assumes square matrix

            int n = mat.Length;  // assumes mat is nxn
            int nCols = mat[0].Length;
            if (n != nCols) Console.WriteLine("M not square ");

            double[][] Q = MatIdentity(n);
            double[][] R = MatCopy(mat);
            for (int i = 0; i < n - 1; ++i)
            {
                double[][] H = MatIdentity(n);
                double[] a = new double[n - i];
                int k = 0;
                for (int ii = i; ii < n; ++ii)
                    a[k++] = R[ii][i];

                double normA = VecNorm(a);
                if (a[0] < 0.0) { normA = -normA; }
                double[] v = new double[a.Length];
                for (int j = 0; j < v.Length; ++j)
                    v[j] = a[j] / (a[0] + normA);
                v[0] = 1.0;

                double[][] h = MatIdentity(a.Length);
                double vvDot = VecDot(v, v);
                double[][] alpha = VecToMat(v, v.Length, 1);
                double[][] beta = VecToMat(v, 1, v.Length);
                double[][] aMultB = MatProduct(alpha, beta);

                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        h[ii][jj] -= (2.0 / vvDot) * aMultB[ii][jj];

                // copy h into lower right of H
                int d = n - h.Length;
                for (int ii = 0; ii < h.Length; ++ii)
                    for (int jj = 0; jj < h[0].Length; ++jj)
                        H[ii + d][jj + d] = h[ii][jj];

                Q = MatProduct(Q, H);
                R = MatProduct(H, R);
            } // i

            if (standardize == true)
            {
                // standardize so R diagonal is all positive
                double[][] D = MatCreate(n, n);
                for (int i = 0; i < n; ++i)
                {
                    if (R[i][i] < 0.0) D[i][i] = -1.0;
                    else D[i][i] = 1.0;
                }
                Q = MatProduct(Q, D);
                R = MatProduct(D, R);
            }

            q = Q;
            r = R;

        } // QR decomposition

        // ------------------------------------------------------

        static void Eigen(double[][] M,
          out double[] eigenVals, out double[][] eigenVecs)
        {
            // compute eigenvalues eigenvectors at the same time

            int n = M.Length;
            double[][] X = MatCopy(M);  // mat must be square
            double[][] Q; double[][] R;
            double[][] pq = MatIdentity(n);
            int maxCt = 10000;

            int ct = 0;
            while (ct < maxCt)
            {
                MatDecomposeQR(X, out Q, out R, false);
                pq = MatProduct(pq, Q);
                X = MatProduct(R, Q);  // note order
                ++ct;

                if (MatIsUpperTri(X, 1.0e-8) == true)
                    break;
            }

            // eigenvalues are diag elements of X
            double[] evals = new double[n];
            for (int i = 0; i < n; ++i)
                evals[i] = X[i][i];

            // eigenvectors are columns of pq
            double[][] evecs = MatCopy(pq);

            eigenVals = evals;
            eigenVecs = evecs;
        }

        // ------------------------------------------------------

        static double[][] MatCreate(int rows, int cols)
        {
            double[][] result = new double[rows][];
            for (int i = 0; i < rows; ++i)
                result[i] = new double[cols];
            return result;
        }

        // ------------------------------------------------------

        static double[][] MatCopy(double[][] m)
        {
            int nRows = m.Length; int nCols = m[0].Length;
            double[][] result = MatCreate(nRows, nCols);
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = m[i][j];
            return result;
        }

        // ------------------------------------------------------

        static double[][] MatProduct(double[][] matA,
          double[][] matB)
        {
            int aRows = matA.Length;
            int aCols = matA[0].Length;
            int bRows = matB.Length;
            int bCols = matB[0].Length;
            if (aCols != bRows)
                throw new Exception("Non-conformable matrices");

            double[][] result = MatCreate(aRows, bCols);

            for (int i = 0; i < aRows; ++i) // each row of A
                for (int j = 0; j < bCols; ++j) // each col of B
                    for (int k = 0; k < aCols; ++k)
                        result[i][j] += matA[i][k] * matB[k][j];

            return result;
        }

        // ------------------------------------------------------

        static bool MatIsUpperTri(double[][] mat,
          double tol)
        {
            int n = mat.Length;
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < i; ++j)
                {  // check lower vals
                    if (Math.Abs(mat[i][j]) > tol)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // ------------------------------------------------------

        static double[][] MatIdentity(int n)
        {
            double[][] result = MatCreate(n, n);
            for (int i = 0; i < n; ++i)
                result[i][i] = 1.0;
            return result;
        }

        // ------------------------------------------------------

        static double[][] MatTranspose(double[][] m)
        {
            int nr = m.Length;
            int nc = m[0].Length;
            double[][] result = MatCreate(nc, nr);  // note
            for (int i = 0; i < nr; ++i)
                for (int j = 0; j < nc; ++j)
                    result[j][i] = m[i][j];
            return result;
        }

        // ------------------------------------------------------

        static void MatShow(double[][] m, int dec, int wid)
        {
            for (int i = 0; i < m.Length; ++i)
            {
                for (int j = 0; j < m[0].Length; ++j)
                {
                    double v = m[i][j];
                    if (Math.Abs(v) < 1.0e-5) v = 0.0;
                    Console.Write(v.ToString("F" + dec).PadLeft(wid));
                }
                Console.WriteLine("");
            }
        }

        // ------------------------------------------------------

        static double VecDot(double[] v1, double[] v2)
        {
            double result = 0.0;
            int n = v1.Length;
            for (int i = 0; i < n; ++i)
                result += v1[i] * v2[i];
            return result;
        }

        // ------------------------------------------------------

        static double VecNorm(double[] vec)
        {
            int n = vec.Length;
            double sum = 0.0;
            for (int i = 0; i < n; ++i)
                sum += vec[i] * vec[i];
            return Math.Sqrt(sum);
        }

        // ------------------------------------------------------

        static double[][] VecToMat(double[] vec,
          int nRows, int nCols)
        {
            double[][] result = MatCreate(nRows, nCols);
            int k = 0;
            for (int i = 0; i < nRows; ++i)
                for (int j = 0; j < nCols; ++j)
                    result[i][j] = vec[k++];
            return result;
        }

        // ------------------------------------------------------

        static void VecShow(double[] vec,
          int dec, int wid)
        {
            for (int i = 0; i < vec.Length; ++i)
            {
                double x = vec[i];
                if (Math.Abs(x) < 1.0e-8) x = 0.0;
                Console.Write(x.ToString("F" + dec).
                  PadLeft(wid));
            }
            Console.WriteLine("");
        }

    } // Program
} // ns
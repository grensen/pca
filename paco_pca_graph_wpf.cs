using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text;

public class TheWindow : Window
{
    // colors
    readonly SolidColorBrush font = new(Color.FromRgb(230, 230, 230));
    // support 
    readonly System.Globalization.CultureInfo ci = System.Globalization.CultureInfo.GetCultureInfo("en-us");
    readonly Typeface tf = new("TimesNewRoman"); // "Arial" // TimesNewRoman

    // layout
    Canvas canGlobal = new Canvas(), canVisual = new(), canCurrent = new(), canRuler = new();

    double fs = 0; // feature size
    double height = 0;

    int dataStateCnt = 0, styleCnt = 0;
    int ggCount = 0;
    int features = 0, labelNum = 0;
    int ys = 40; // y start point
    int xs = 20; // x start point menu
    float[] minVal = new float[49], maxVal = new float[49];
    float minAll = float.MaxValue, maxAll = float.MinValue;

    bool[] featureState = new bool[49];
    bool[] isLabel = new bool[10];
    float[] trainData;
    int[] trainLabels;

    bool regression = false;
    Brush[] br = new Brush[10];
    string name = "";
    string[] featureNames = null;
    string classesInfo = "";

    [STAThread]
    public static void Main() { new Application().Run(new TheWindow()); }

    // CONSTRUCTOR - LOADED - ONINIT
    private TheWindow() // constructor
    {
        // set window   
        Title = "Parallel coordinates 23"; //        
        Content = canGlobal;
        Background = RGB(0, 0, 0);
        Width = 960;
        Height = 840;

        SizeChanged += Window_SizeChanged;
        MouseDown += Mouse_Down;

        canGlobal.Children.Add(canVisual);
        canGlobal.Children.Add(canCurrent);
        canGlobal.Children.Add(canRuler);
        DataInit();
        ColorInit();
        return;
        // continue in Window_SizeChanged()...
    } // TheWindow end

    bool clicked = false;
    int lastPoint = 0;
    int userY = 0;
    // List<Tuple<int, int>> rules = new List<Tuple<int, int>>();

    void Mouse_Down(object sender, MouseButtonEventArgs e)
    {
        int gpy = (int)e.GetPosition(this).Y, gpx = (int)e.GetPosition(this).X;

        clicked = true;
        userY = gpy;

        ButtonActions();

        void ButtonActions()
        {
            if (gpy > ys + 0 && gpy < ys + 0 + 20 && gpx > xs && gpx < xs + 20)
            {
                dataStateCnt++;
                DataInit();
                DrawParallelCoordinates();
            }
            for (int i = 0; i < labelNum; i++)
                if (gpy > ys + 30 + i * 30 && gpy < ys + 30 + i * 30 + 20 && gpx > xs && gpx < xs + 20)
                {
                    isLabel[i] = !isLabel[i];
                    DrawParallelCoordinates(); break;
                }
            int ys2 = labelNum * 30; // class dist y for n classes * 30 pixels 

            if (gpy > ys + ys2 + 60 && gpy < ys + ys2 + 60 + 20 && gpx > xs && gpx < xs + 20)
            {
                styleCnt++;
                DrawParallelCoordinates();
            }
            if (gpy > ys + ys2 + 90 && gpy < ys + ys2 + 90 + 20 && gpx > xs && gpx < xs + 20)
            {
                DrawParallelCoordinates(true); // shuffle data
            }

            if (gpy > ys + ys2 + 120 && gpy < ys + ys2 + 120 + 20 && gpx > xs && gpx < xs + 20)
            {

                //  minAll = float.MaxValue;
                //  maxAll = float.MinValue;

                if (regression)
                {
                    for (int i = 0; i < features - 1; i++)
                    {

                        if (minVal[i] < minAll) { minAll = minVal[i]; }
                        if (maxVal[i] > maxAll) { maxAll = maxVal[i]; }
                    }
                    for (int i = 0; i < features - 1; i++) minVal[i] = minAll;
                    for (int i = 0; i < features - 1; i++) maxVal[i] = maxAll;
                }
                else
                {
                    for (int i = 0; i < features; i++)
                    {
                        if (minVal[i] < minAll) { minAll = minVal[i]; }
                        if (maxVal[i] > maxAll) { maxAll = maxVal[i]; }
                    }
                    // check min and max for all features
                    for (int i = 0; i < features; i++) minVal[i] = minAll;
                    for (int i = 0; i < features; i++) maxVal[i] = maxAll;
                }


                // DataInit();

                DrawParallelCoordinates(); // shuffle data
            }
            // on off features
            for (int i = 0; i < features; i++)
                if (gpy > ys + ys2 + height + 25 && gpy < ys + ys2 + height + 25 + 20 && gpx > xs + i * fs && gpx < xs + i * fs + 20)
                {
                    featureState[i] = !featureState[i];
                    DrawParallelCoordinates(); break;
                }
        }
    }
    void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetWindow();
        DrawParallelCoordinates();
    } // Window_SizeChanged end

    void DrawParallelCoordinates(bool dataShuffle = false)
    {
        int len = trainLabels.Length >= 1000 ? 1000 * features : trainData.Length;
        DrawingContext dc = ContextHelpMod(false, ref canVisual);
        BackgroundStuff(ref dc, features, len);
        int[] ids = new int[trainLabels.Length];
        Shuffle(ids, dataShuffle, new Random(ggCount++));
        styleCnt = styleCnt > 2 ? 0 : styleCnt;

        double yh = ys + height;
        float[] maxMinusMin = new float[maxVal.Length];
        for (int i = 0; i < maxMinusMin.Length; i++)
            maxMinusMin[i] = 1.0f / (maxVal[i] - minVal[i]);

        double lineHeight = (height / (labelNum - 1));

        for (int i = 0, lb = 0; i < len; i += features, lb++)
        {
            int lab = trainLabels[ids[lb]];
            if (!isLabel[lab]) continue;

            int id = ids[lb] * features;
            //  Pen pen = pens[lab];
            Brush clr = br[lab];
            double startLine = ys + lab * lineHeight;
            Pen pen = new(clr, 0.5);

            if (styleCnt == 0)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * ((trainData[id + j] - minVal[j]) * (maxMinusMin[j]));
                    Line2(dc, pen, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos);
                    startLine = pixelPos;
                    double SetLine()
                    {

                        //  double pixelPos = yh - height * ((trainData[id + j] - minVal[j]) * (maxMinusMin[j]));
                        Line2(dc, pen, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos);
                        return pixelPos;
                    }
                }
            else if (styleCnt == 1)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * ((trainData[id + j] - minVal[j]) * (maxMinusMin[j]));
                    Line2(dc, pen, 15 + j * fs, startLine, 15 + (j + 1) * fs, pixelPos);
                }
            else if (styleCnt == 2)
                for (int j = 0; j < features; j++)
                {
                    if (!featureState[j]) continue;
                    double pixelPos = yh - height * ((trainData[id + j] - minVal[j]) * (maxMinusMin[j]));
                    Line2(dc, pen, 15 + j * fs, pixelPos, 15 + (j + 1) * fs, pixelPos);
                }
        }
        int graphX = 300;

        int ft1, ft2;
        if (dataStateCnt == 0)
        {
            ft1 = 0; ft2 = 1;
            Text(ref dc, "PCA Feature X = " + ft1.ToString() + ", PCA Feature Y = " + ft2.ToString(), 12, font, 70, 450 - 20);

        }
        else
        {
            ft1 = 2; ft2 = 3;
            Text(ref dc, "Feature X =" + featureNames[ft1] + ", Feature Y =" + featureNames[ft2], 12, font, 70, 450 - 20);

        }

        dc.DrawLine(new Pen(font, 1.0), new Point(60, 450), new Point(60 + graphX, 450));
        dc.DrawLine(new Pen(font, 1.0), new Point(60 + graphX, 450), new Point(60 + graphX, 450 + graphX));
        dc.DrawLine(new Pen(font, 1.0), new Point(60, 450), new Point(60, 450 + graphX));
        dc.DrawLine(new Pen(font, 1.0), new Point(60, 450 + graphX), new Point(60 + graphX, 450 + graphX));

        {
            // ft1
            {

                int j = 0;
                dc.DrawLine(new Pen(font, 1.0), new Point(15 + (j + 1) * fs, ys), new Point(15 + (j + 1) * fs, ys + height));
                var min = minVal[ft1];
                double range = maxVal[ft1] - min;
                for (int i = 0, cats = 10; i < cats + 1; i++) // accuracy lines 0, 20, 40...
                {
                    double yGrph = 15 + 450 + graphX;
                    Text(ref dc, (range / cats * i + min).ToString("F2"), 8, font, 50 + i * (graphX / cats), (int)yGrph - 5);
                }
            }
            // ft2
            {

                int j = 0;
                dc.DrawLine(new Pen(font, 1.0), new Point(15 + (j + 1) * fs, ys), new Point(15 + (j + 1) * fs, ys + height));
                var min = minVal[ft2];
                double range = maxVal[ft2] - min;
                for (int i = 0, cats = 10; i < cats + 1; i++) // accuracy lines 0, 20, 40...
                {
                    double yGrph = 450 + graphX - i * (graphX / cats);
                    Text(ref dc, (range / cats * i + min).ToString("F2"), 8, font, 40, (int)yGrph - 5);
                }
            }

        }

        for (int i = 0, lb = 0; i < len; i += features, lb++)
        {
            int lab = trainLabels[ids[lb]];
            if (!isLabel[lab]) continue;
            int id = ids[lb] * features;
            Brush clr = br[lab];
            dc.DrawEllipse(clr, new Pen(), new Point(
                  60 +  ((trainData[id + ft1] - minVal[ft1]) / (maxVal[ft1] - minVal[ft1])) * graphX
                , 450 + graphX - ((trainData[id + ft2] - minVal[ft2]) / (maxVal[ft2] - minVal[ft2])) * graphX
                )
                , 2, 2);
        }
       
        void Line2(DrawingContext dc, Pen pen, double xl, double yl, double xr, double yr)
        {
            dc.DrawLine(pen, new Point(xl, yl), new Point(xr, yr));
        }
        // values max min visual plus lines  
        for (int j = 0; j < features; j++)
        {
            dc.DrawLine(new Pen(font, 1.0), new Point(15 + (j + 1) * fs, ys), new Point(15 + (j + 1) * fs, ys + height));
            var min = minVal[j];
            double range = maxVal[j] - min;
            for (int i = 0, cats = 20; i < cats + 1; i++) // accuracy lines 0, 20, 40...
            {
                double yGrph = ys + height - i * (height / cats);
                Line(ref dc, font, 1.0, 15 + (j + 1) * fs - 5, yGrph, 15 + (j + 1) * fs, yGrph);
                Text(ref dc, (range / cats * i + min).ToString("F2"), 8, font, (int)((j + 1) * fs - 10), (int)yGrph - 5);
            }
        }

       // dc.DrawEllipse(new Pen(Brushes.Aqua), new Rectangle(9, 9, 160, 600));
        InfoStuff(ref dc, features, ys);
        dc.Close();

        void InfoStuff(ref DrawingContext dc, int inputLen, int ys)
        {
            Text(ref dc, "Data", 10, font, xs + 30, ys + 5); // switch data
            Rect(ref dc, RGB(64, 64, 64), xs, ys + 0, 20, 20);// switch data button

            for (int i = 0; i < labelNum; i++, ys += 30)
            {
                string str = "Class " + i.ToString();
                FontFamily fontFamily = new FontFamily("Arial");
                double fontSize = 10;

                FormattedText formattedText = new FormattedText(
                    str,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                    fontSize,
                    Brushes.Black,
                    new NumberSubstitution(),
                    TextFormattingMode.Display
                );

                double width = formattedText.Width;
                Rect(ref dc, RGB(129, 129, 129), xs + 30, ys + 35, (int)width, 12);
                Text(ref dc, str, 10, br[i], xs + 30, ys + 35);
                //  Text(ref dc, classNames[i], 10, br[i], xs + 30, ys + 35);

                Rect(ref dc, RGB(64, 64, 64), xs - 1, ys + 29, 22, 22); // background class button
                Rect(ref dc, (isLabel[i] ? br[i] : RGB(64, 64, 64)), xs, ys + 30, 20, 20); // class button
            }

            Text(ref dc, "Style", 10, font, xs + 30, ys + 65);
            Text(ref dc, "Shuffle", 10, font, xs + 30, ys + 95);
            Text(ref dc, "Normalize", 10, font, xs + 30, ys + 125);

            Rect(ref dc, RGB(235, 184, 184), xs, ys + 60, 20, 20); // Style
            Rect(ref dc, RGB(184, 235, 184), xs, ys + 90, 20, 20); // Shuffle

            Rect(ref dc, RGB(184, 184, 235), xs, ys + 120, 20, 20); // Normalize

            for (int i = 0; i < -inputLen; i++)
                Rect(ref dc, featureState[i] ? RGB(164, 64, 164) : RGB(16, 16, 16), (int)(xs + i * fs), (int)(ys + height + 25), 20, 20);
        }
        void BackgroundStuff(ref DrawingContext dc, int inputLen, int len)
        {
            string str = ", to predict: " + classesInfo;
            //  for (int i = 1; i < classNames.Length; i++) str += " ," + classNames[i];

            Text(ref dc, name + " dataset with " + (len / inputLen).ToString() + " examples, " + inputLen.ToString()
                + " features and " + labelNum.ToString() + " labels"
                + str, 12, font, (int)(15 + 0 * fs), 10);

            Rect(ref dc, RGB(24, 24, 24), 15, ys, (int)(inputLen * fs), (int)height);

            for (int i = 0; i < inputLen; i++)
                Text(ref dc, i.ToString() + " = " + featureNames[i], 10, Brushes.White, (int)(-15 + i * fs + fs / 2), 25);
            

        }

        void Line(ref DrawingContext dc, Brush rgb, double size, double xl, double yl, double xr, double yr) => dc.DrawLine(new Pen(rgb, size), new Point(xl, yl), new Point(xr, yr));
        void Text(ref DrawingContext dc, string str, int size, Brush rgb, int x, int y) => dc.DrawText(new FormattedText(str, ci, FlowDirection.LeftToRight, tf, size, rgb, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(x, y));
    }
    void Rect(ref DrawingContext dc, Brush rgb, int x, int y, int width, int height) => dc.DrawRectangle(rgb, null, new Rect(x, y, width, height));

    private static void Shuffle(int[] route, bool shuffle, Random rn)
    {
        int n = route.Length;
        for (int i = 0; i < n; i++) route[i] = i;

        if (shuffle)
            for (int i = 0, r = rn.Next(0, n); i < n; i++, r = rn.Next(i, n))
                (route[r], route[i]) = (route[i], route[r]);
    }

    void DataInit()
    {
        trainData = null; trainLabels = null; regression = false;
        dataStateCnt = dataStateCnt > 1 ? 0 : dataStateCnt;

        string testFile = "..\\..\\..\\Data\\iris_train.txt";
        double[][] testX = MatLoad(testFile, new int[] { 0, 1, 2, 3 }, ',', "#");

        if (dataStateCnt == 0) // iris_train
        {
            name = "Iris PCA Trainig";
            labelNum = 3;

            classesInfo = "Species: 0 = setosa, 1 = versicolor, 2 = virginica";

            double[] means;
            double[] stds;
            double[][] stdX = MatStandardize(testX,
              out means, out stds);

            double[][] covarMat = CovarMatrix(stdX, false);

            double[] eigenVals;
            double[][] eigenVecs;
            Eigen(covarMat, out eigenVals, out eigenVecs);

            // sort eigenvals from large to smallest
            int[] idxs = ArgSort(eigenVals);  // save to sort evecs
            Array.Reverse(idxs);

            Array.Sort(eigenVals);
            Array.Reverse(eigenVals);

            eigenVecs = MatExtractCols(eigenVecs, idxs);  // sort 
            eigenVecs = MatTranspose(eigenVecs);  // as rows

            double sum = 0.0;
            for (int i = 0; i < eigenVals.Length; ++i)
                sum += eigenVals[i];
            for (int i = 0; i < eigenVals.Length; ++i)
            {
                double pctExplained = eigenVals[i] / sum;
            }
            int k = 2;
            int[] dimensions = new int[k];
            for (int i = 0; i < dimensions.Length; i++)
                dimensions[i] = i;

            double[][] transformed =
              MatProduct(stdX, MatTranspose(eigenVecs));  // all 
            double[][] reduced = MatExtractCols(transformed,
              dimensions);  // first 2 

            string attributes = "PCA Feature 0, PCA Feature 1, PCA Feature 3, PCA Feature 4";
            featureNames = attributes.Substring(0).Split(',');

            features = reduced[0].Length;

            float[] reducedF = new float[reduced.Length * features];
            trainData = new float[reduced.Length * reduced[0].Length];
            trainLabels = new int[reduced.Length];
            //  trainLabels = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();
            //  trainData = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
            // trainData = 
            for (int i = 0, c = 0; i < testX.Length; i++)
            {
                for (int j = 0; j < features; j++)
                    trainData[c++] = (float)reduced[i][j];
                trainLabels[i] = (int)testX[i][^1];
            }

        } // iris
        // 
        if (dataStateCnt == 1) // iris_train
        {

            name = "Iris Trainig ";
            labelNum = 3;
            features = 4;
            /*
            var trainPath = @"C:\datasets\iris_train.txt";
            trainData = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
            trainLabels = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();
            */
            float[] reducedF = new float[testX.Length * features];
            trainData = new float[testX.Length * testX[0].Length];
            trainLabels = new int[testX.Length];
            //  trainLabels = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).Select(values => int.Parse(values.Last())).ToArray();
            //  trainData = File.ReadAllLines(trainPath).Skip(3).Select(line => line.Split(',')).SelectMany(values => values.Skip(0).Take(4).Select(float.Parse)).ToArray();
            // trainData = 
            for (int i = 0, c = 0; i < testX.Length; i++)
            {
                for (int j = 0; j < features; j++)
                    trainData[c++] = (float)testX[i][j];
                trainLabels[i] = (int)testX[i][^1];
            }
            // 
            // Sepal length, Sepal width, Petal length, Petal width
            // attributes - sepal length, sepal width, petal length, petal width
            string attributes = "sepal length, sepal width, petal length, petal width";
            featureNames = attributes.Substring(0).Split(',');
            classesInfo = "Species: 0 = setosa, 1 = versicolor, 2 = virginica";


        } // iris


        minVal = new float[features];
        maxVal = new float[features];
        featureState = new bool[features];
        for (int vals = 0; vals < features; vals++)
        {
            float cmin = float.MaxValue, cmax = float.MinValue;
            for (int dt = 0; dt < trainLabels.Length; dt++)
            {
                var val = trainData[vals + features * dt];
                if (val < cmin) { cmin = val; }
                if (val > cmax) { cmax = val; }
            }
            minVal[vals] = cmin; maxVal[vals] = cmax;
        }


        for (int i = 0; i < features; i++) featureState[i] = true;
        isLabel = new bool[10];
        for (int i = 0; i < 10; i++) isLabel[i] = true;



        SetWindow();
    }

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
    void SetWindow()
    {
        fs = (((Canvas)this.Content).RenderSize.Width - 30) / features;
        height = ((Canvas)this.Content).RenderSize.Height - 460;
    }

    static DrawingContext ContextHelpMod(bool isInit, ref Canvas cTmp)
    {
        if (!isInit) cTmp.Children.Clear();
        DrawingVisualElement drawingVisual = new();
        cTmp.Children.Add(drawingVisual);
        return drawingVisual.drawingVisual.RenderOpen();
    }
    void ColorInit()
    {
        br[0] = RGB(50, 70, 255); // blue
        br[1] = RGB(255, 188, 0); // gold
        br[2] = RGB(255, 0, 0); // red
        br[3] = RGB(161, 195, 255); // baby blue                  
        br[4] = RGB(0, 255, 0); // green
        br[5] = RGB(255, 0, 255); // magenta
        br[6] = RGB(75, 0, 130); // indigo
        br[7] = RGB(0, 128, 128); // teal
        br[8] = RGB(128, 128, 70); // olive
        br[9] = RGB(255, 222, 0); // yellow
    }
    public static Brush InterpolateColor(float value, float min, float max)
    {
        byte minR = 25 + 16;
        byte minG = 25;
        byte minB = 230;
        byte maxR = 255;
        byte maxG = 186 + 16;
        byte maxB = 0;

        float boostFactor = (value - min) / (max - min);
        byte r = InterpolateByte(minR, maxR, boostFactor);
        byte g = InterpolateByte(minG, maxG, boostFactor);
        byte b = InterpolateByte(minB, maxB, boostFactor);

        return RGB(r, g, b);
    }
    private static byte InterpolateByte(byte minValue, byte maxValue, double boostFactor)
    {
        return (byte)(boostFactor * maxValue + (1 - boostFactor) * minValue);
    }
    public static Brush RGB(byte red, byte green, byte blue)
    {
        Brush brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
} // TheWindow end

public class DrawingVisualElement : FrameworkElement
{
    private readonly VisualCollection _children;
    public DrawingVisual drawingVisual;

    public DrawingVisualElement()
    {
        _children = new VisualCollection(this);
        drawingVisual = new DrawingVisual();
        _children.Add(drawingVisual);
    }

    public void ClearVisualElement()
    {
        _children.Clear();
    }

    protected override int VisualChildrenCount => _children.Count;

    protected override Visual GetVisualChild(int index)
    {
        if (index < 0 || index >= _children.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }
}


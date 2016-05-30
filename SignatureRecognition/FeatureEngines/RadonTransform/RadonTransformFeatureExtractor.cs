using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;
using FeatureEngines.Helpers;
using static System.Math;
using System.Linq;

namespace FeatureEngines.RadonTransform
{
    public class RadonTransformFeatureExtractor : IFeatureExtractor
    {
        #region consts

        private double cos45, sin45, cos22, sin22, sec22, cos67, sin67, sqrt2, halfsqrt2, halfsqrt2divsin22, doublesqrt2;


        [DefaultValue(10)]
        public int halfBeams { get; set; }
        private int beams { get; set; }

        int numPixels, subBeams, totalSubBeams, halfSubBeams;
        int[] centroid = new int[2];
        double radius, beamWidth;

        private double[,] sbTotals;
        private double[,] bTotals;

        #endregion

        #region constructor

        public RadonTransformFeatureExtractor(int halfBeams = 10)
        {
            this.halfBeams = halfBeams;
            beams = 2*halfBeams + 1;

            sbTotals = new double[8, 100000];       //fixed size to avoid allocation of memory during each step (max 1000 sub-beams)
            bTotals = new double[8, beams];
            var pi = PI;
            sqrt2 = Sqrt(2);
            halfsqrt2 = 0.5f * sqrt2;
            halfsqrt2divsin22 = (0.5 * sqrt2 / Sin(pi / 8));
            doublesqrt2 = 2 * sqrt2;
            cos45 = sqrt2 / 2;
            sin45 = cos45;
            cos22 = Cos(pi / 8);
            sin22 = Sin(pi / 8);
            sec22 = 1 / cos22;
            cos67 = Cos(3 * pi / 8);
            sin67 = Sin(3 * pi / 8);
        }

        public RadonTransformFeatureExtractor()
        {
            var pi =  PI;
            sqrt2 = Sqrt(2);
            halfsqrt2 = 0.5f*sqrt2;
            halfsqrt2divsin22 = (0.5*sqrt2/Sin(pi/8));
            doublesqrt2 = 2*sqrt2;
            cos45 = sqrt2/2;
            sin45 = cos45;
            cos22 =  Cos(pi/8);
            sin22 = Sin(pi/8);
            sec22 = 1/cos22;
            cos67 = Cos(3*pi/8);
            sin67 = Sin(3*pi/8);
        }

        #endregion

        public Feature Extract(string file)
        {
            var bitmap = new Bitmap(file);

            List<Pixel> signaturePixels = GetPixels(bitmap);

            Transform(signaturePixels);

           // return new Feature() {Data = (float[,])bTotals};
            return new Feature() {Data = (double[,])bTotals.Clone()};
        }

        public DataSet GetTeachingDataset(string[] files, string[] forgeries = null)
        {
            if(forgeries == null)
                forgeries = new string[0];
            var dataSet = new DataSet();
            foreach (var file in files)
            {
                dataSet.Signatures.Add(Extract(file));
            }
            foreach (var forgery in forgeries)
            {
                dataSet.Forgeries.Add(Extract(forgery));
            }

            //WTF is going on?
            double max = 0;
            foreach (var st in dataSet.Signatures)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 21; j++)
                        if (max < st.Data[i, j])
                            max = st.Data[i, j];
                }
            }
            foreach (var st in dataSet.Forgeries)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 21; j++)
                        if (max < st.Data[i, j])
                            max = st.Data[i, j];
                }
            }

            //foreach (var st in dataSet.Signatures)
            //{
            //    for (int i = 0; i < 8; i++)
            //    {
            //        for (int j = 0; j < 21; j++)
            //            st.Data[i, j] /= max;
            //    }
            //}
            //foreach (var st in dataSet.Forgeries)
            //{
            //    for (int i = 0; i < 8; i++)
            //    {
            //        for (int j = 0; j < 21; j++)
            //            st.Data[i, j] /= max;
            //    }
            //}

            return dataSet;
        }

        private List<Pixel> GetPixels(Bitmap bitmap)
        {
            var signaturePixels = new List<Pixel>();
            for(var x = 0; x < bitmap.Width; x++)
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var color = bitmap.GetPixel(x, y);
                    if(color.R < 122 && color.G < 122 && color.B < 122 )
                        signaturePixels.Add(new Pixel(x,y));
                }

            return signaturePixels;
        }

        private void Transform(List<Pixel> pixels)
        {
            Prepare(pixels);

            foreach (var pixel in pixels)
            {
                Project0Degrees(pixel);
                Project22Degrees(pixel);
                Project45Degrees(pixel);
                Project67Degrees(pixel);
                Project90Degrees(pixel);
                Project112Degrees(pixel);
                Project135Degrees(pixel);
                Project157Degrees(pixel);
            }
            FormatsbTotals();
            var lol = new List<string>();

            for (var j=0; j < 51 * 8;j += 51)
            {
                var debug = bTotals.Cast<double>().Skip(j).Take(51).Select(i => i.ToString());
                var output = String.Join("\t", debug);
                lol.Add(output);
            }

            
            //System.IO.File.WriteAllLines("scores.txt", lol);
            int max = 0;
            for (int i = 0; i < 100000; i++)
            {
                if (sbTotals[2, i] != 0)
                    max = i;
            }
            ;
        }

        private void CalculateCentroid(List<Pixel> pixels)
        {
            int sumX = 0, sumY = 0;

            foreach (var pixel in pixels)
            {
                sumX += pixel.X;
                sumY += pixel.Y;
            }

            centroid[0] = sumX/numPixels;
            centroid[1] = sumY/numPixels;
        }

        private void CalculateRadius(List<Pixel> pixels)
        {
            radius = 0;

            foreach (var pixel in pixels)
            {
                var len = Pow(pixel.X - centroid[0], 2) + Pow(pixel.Y - centroid[1], 2);
                radius = Max(radius, len);
            }
            radius = Sqrt(radius) + halfsqrt2;
        }

        private void Prepare(List<Pixel> pixels)
        {
            numPixels = pixels.Count;

            CalculateCentroid(pixels);
            CalculateRadius(pixels);

            subBeams = (int)(2 * Ceiling( (10 * radius + halfBeams + 1) / beams) +1);
            totalSubBeams = beams*subBeams;
            halfSubBeams = (totalSubBeams - 1)/2;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < totalSubBeams; j++)
                {
                    sbTotals[i, j] = 0;
                }
                for (int j = 0; j < beams; j++)
                {
                    bTotals[i, j] = 0;
                }
            }

            beamWidth = radius/halfSubBeams;
        }

        #region projections

        private void Project0Degrees(Pixel pixel)
        {
            var c = (pixel.X - centroid[0])/beamWidth;

            int l = (int)Ceiling(c - 0.5/beamWidth) + halfSubBeams;
            int r = (int)Ceiling(c + 0.5/beamWidth) + halfSubBeams;

            for (var i = l; i < r; i++)
            {
                sbTotals[0, i] += 1;
            }
        }

        private void Project22Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = ((pixel.Y - centroid[1] - 0.5) * cos22 + (pixel.X - centroid[0] - 0.5) * sin22) / beamWidth;
            double cr = ((pixel.Y - centroid[1] + 0.5) * cos22 + (pixel.X - centroid[0] + 0.5) * sin22) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //length of incline
            double incl = (int)Floor(0.293 * (cr - cl + 1));

            //add contributions to sub-beams
            for (int i = l; i <= l + incl - 1; i++)
            {
                sbTotals[1,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }

            for (int i = l + (int)incl; i <= r - incl; i++)
            {
                sbTotals[1,i] += sec22;
            }

            for (int i = r - (int)incl + 1; i <= r; i++)
            {
                sbTotals[1,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        private void Project45Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = ((pixel.Y - centroid[1] - 0.5) * cos45 + (pixel.X - centroid[0] - 0.5) * sin45) / beamWidth;
            double cr = ((pixel.Y - centroid[1] + 0.5) * cos45 + (pixel.X - centroid[0] + 0.5) * sin45) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //add contributions to sub-beams
            for (int i = l; i <= r; i++)
            {
                sbTotals[2,i] += sqrt2 - 2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        private void Project67Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = ((pixel.Y - centroid[1] - 0.5) * cos67 + (pixel.X - centroid[0] - 0.5) * sin67) / beamWidth;
            double cr = ((pixel.Y - centroid[1] + 0.5) * cos67 + (pixel.X - centroid[0] + 0.5) * sin67) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //length of incline
            float incl = (float)Floor(0.293 * (cr - cl + 1));

            //add contributions to sub-beams
            for (int i = l; i <= l + incl - 1; i++)
            {
                sbTotals[3,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }

            for (int i = l + (int)incl; i <= r - incl; i++)
            {
                sbTotals[3,i] += sec22;
            }

            for (int i = r - (int)incl + 1; i <= r; i++)
            {
                sbTotals[3,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        private void Project90Degrees(Pixel pixel)
        {
            //projection of center of pixel on new axis
            double c = (pixel.Y - centroid[1]) / beamWidth;

            //rounded projected left and right corners of pixel
            int l = (int)Ceiling(c - 0.5 / beamWidth) + halfSubBeams;
            int r = (int)Floor(c + 0.5 / beamWidth) + halfSubBeams;

            //add contributions to sub-beams
            for (int i = l; i <= r; i++)
            {
                sbTotals[4,i] += 1;
            }
        }

        private void Project112Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = (-(pixel.Y - centroid[1] + 0.5) * cos22 + (pixel.X - centroid[0] - 0.5) * sin22) / beamWidth;
            double cr = (-(pixel.Y - centroid[1] - 0.5) * cos22 + (pixel.X - centroid[0] + 0.5) * sin22) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //length of incline
            float incl = (float)Floor(0.293 * (cr - cl + 1));

            //add contributions to sub-beams
            for (int i = l; i <= l + incl - 1; i++)
            {
                sbTotals[5,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }

            for (int i = l + (int)incl; i <= r - incl; i++)
            {
                sbTotals[5,i] += sec22;
            }

            for (int i = r - (int)incl + 1; i <= r; i++)
            {
                sbTotals[5,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        private void Project135Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = (-(pixel.Y - centroid[1] + 0.5) * cos45 + (pixel.X - centroid[0] - 0.5) * sin45) / beamWidth;
            double cr = (-(pixel.Y - centroid[1] - 0.5) * cos45 + (pixel.X - centroid[0] + 0.5) * sin45) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //add contributions to sub-beams
            for (int i = l; i <= r; i++)
            {
                sbTotals[6,i] += sqrt2 - 2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        private void Project157Degrees(Pixel pixel)
        {
            //projected left & right corners of pixel
            double cl = (-(pixel.Y - centroid[1] + 0.5) * cos67 + (pixel.X - centroid[0] - 0.5) * sin67) / beamWidth;
            double cr = (-(pixel.Y - centroid[1] - 0.5) * cos67 + (pixel.X - centroid[0] + 0.5) * sin67) / beamWidth;

            //average of cl and cr (center pixel of pixel)
            double c = (cl + cr) / 2;

            //rounded values - left and right sub-beams affected
            int l = (int)Ceiling(cl) + halfSubBeams;
            int r = (int)Floor(cr) + halfSubBeams;

            //length of incline
            float incl = (int)Floor(0.293 * (cr - cl + 1));

            //add contributions to sub-beams
            for (int i = l; i <= l + incl - 1; i++)
            {
                sbTotals[7,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }

            for (int i = l + (int)incl; i <= r - incl; i++)
            {
                sbTotals[7,i] += sec22;
            }

            for (int i = r - (int)incl + 1; i <= r; i++)
            {
                sbTotals[7,i] += halfsqrt2divsin22 - doublesqrt2 * Abs(i - halfSubBeams - c) * beamWidth;
            }
        }

        #endregion

        private void FormatsbTotals()
        {
            double scalingFactor = beamWidth / numPixels;

            //for each projection
            for (int h = 0; h < 8; h++)
            {
                //beam counter
                int beam = 0;

                //for each beam grouping
                for (int i = 0; i < totalSubBeams; i += subBeams)
                {
                    double sum = 0;

                    //add all the beams together in grouping
                    for (int j = i; j < i + subBeams; j++) sum += sbTotals[h,j];

                    //store in final result with scaling
                    bTotals[h,beam] = sum * scalingFactor;
                    beam++;
                }
            }

            /*//debug: print sum of beams
            for ( int h=0; h < 8; h++ )
            {
                float sum = 0;

                for (int i=0; i<beams; i++) 
                {			
                    sum += bTotals[h][i];
                }

                cout << "Projection " << h << ": " << sum << endl;
            }*/
        }
    }
}
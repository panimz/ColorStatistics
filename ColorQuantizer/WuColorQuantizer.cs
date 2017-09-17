using System;
using System.Drawing;

namespace ColorQuantizer
{
    /// <summary>
    /// A Wu's color quantizer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Based on C Implementation of Xiaolin Wu's Color Quantizer (v. 2)
    /// (see Graphics Gems volume II, pages 126-133)
    /// (<see href="http://www.ece.mcmaster.ca/~xwu/cq.c"/>).
    /// </para>
    /// <para>
    /// Algorithm: Greedy orthogonal bipartition of RGB space for variance
    /// minimization aided by inclusion-exclusion tricks.
    /// For speed no nearest neighbor search is done. Slightly
    /// better performance can be expected by more sophisticated
    /// but more expensive versions.
    /// </para>
    /// </remarks>
    public sealed class WuColorQuantizer : IColorQuantizer
    {
        /// <summary>
        /// The index bits.
        /// </summary>
        private const int IndexBits = 7;

        /// <summary>
        /// The index count.
        /// </summary>
        private const int IndexCount = (1 << IndexBits) + 1;

        /// <summary>
        /// The table length.
        /// </summary>
        private const int TableLength = IndexCount * IndexCount * IndexCount;

        /// <summary>
        /// Moment of <c>P(c)</c>.
        /// </summary>
        private readonly long[] vwt = new long[TableLength];

        /// <summary>
        /// Moment of <c>r*P(c)</c>.
        /// </summary>
        private readonly long[] vmr = new long[TableLength];

        /// <summary>
        /// Moment of <c>g*P(c)</c>.
        /// </summary>
        private readonly long[] vmg = new long[TableLength];

        /// <summary>
        /// Moment of <c>b*P(c)</c>.
        /// </summary>
        private readonly long[] vmb = new long[TableLength];

        /// <summary>
        /// Moment of <c>c^2*P(c)</c>.
        /// </summary>
        private readonly double[] m2 = new double[TableLength];

        /// <summary>
        /// Color space tag.
        /// </summary>
        private readonly byte[] tag = new byte[TableLength];

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB).</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image)
        {
            return Quantize(image, 256);
        }

        /// <summary>
        /// Quantizes an image.
        /// </summary>
        /// <param name="image">The image (XRGB).</param>
        /// <param name="colorCount">The color count.</param>
        /// <returns>The result.</returns>
        public ColorQuantizerResult Quantize(byte[] image, int colorCount)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            if (colorCount < 1 || colorCount > 256)
            {
                throw new ArgumentOutOfRangeException("colorCount");
            }

            Clear();

            Build3DHistogram(image);
            Get3DMoments();

            var cube = BuildCube(ref colorCount);
            return GenerateResult(image, colorCount, cube);
        }

        /// <summary>
        /// Gets an index.
        /// </summary>
        /// <param name="r">The red value.</param>
        /// <param name="g">The green value.</param>
        /// <param name="b">The blue value.</param>
        /// <returns>The index.</returns>
        private static int GetIndex(int r, int g, int b)
        {
            return (r << (IndexBits * 2)) + (r << (IndexBits + 1)) + (g << IndexBits) + r + g + b;
        }

        /// <summary>
        /// Computes sum over a box of any given statistic.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static double Volume(ColorRange cube, long[] moment)
        {
            return moment[GetIndex(cube.R1, cube.G1, cube.B1)]
               - moment[GetIndex(cube.R1, cube.G1, cube.B0)]
               - moment[GetIndex(cube.R1, cube.G0, cube.B1)]
               + moment[GetIndex(cube.R1, cube.G0, cube.B0)]
               - moment[GetIndex(cube.R0, cube.G1, cube.B1)]
               + moment[GetIndex(cube.R0, cube.G1, cube.B0)]
               + moment[GetIndex(cube.R0, cube.G0, cube.B1)]
               - moment[GetIndex(cube.R0, cube.G0, cube.B0)];
        }

        /// <summary>
        /// Computes part of Volume(cube, moment) that doesn't depend on r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Bottom(ColorRange cube, int direction, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return -moment[GetIndex(cube.R0, cube.G1, cube.B1)]
                        + moment[GetIndex(cube.R0, cube.G1, cube.B0)]
                        + moment[GetIndex(cube.R0, cube.G0, cube.B1)]
                        - moment[GetIndex(cube.R0, cube.G0, cube.B0)];

                // Green
                case 1:
                    return -moment[GetIndex(cube.R1, cube.G0, cube.B1)]
                        + moment[GetIndex(cube.R1, cube.G0, cube.B0)]
                        + moment[GetIndex(cube.R0, cube.G0, cube.B1)]
                        - moment[GetIndex(cube.R0, cube.G0, cube.B0)];

                // Blue
                case 0:
                    return -moment[GetIndex(cube.R1, cube.G1, cube.B0)]
                        + moment[GetIndex(cube.R1, cube.G0, cube.B0)]
                        + moment[GetIndex(cube.R0, cube.G1, cube.B0)]
                        - moment[GetIndex(cube.R0, cube.G0, cube.B0)];

                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        /// <summary>
        /// Computes remainder of Volume(cube, moment), substituting position for r1, g1, or b1 (depending on direction).
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="position">The position.</param>
        /// <param name="moment">The moment.</param>
        /// <returns>The result.</returns>
        private static long Top(ColorRange cube, int direction, int position, long[] moment)
        {
            switch (direction)
            {
                // Red
                case 2:
                    return moment[GetIndex(position, cube.G1, cube.B1)]
                       - moment[GetIndex(position, cube.G1, cube.B0)]
                       - moment[GetIndex(position, cube.G0, cube.B1)]
                       + moment[GetIndex(position, cube.G0, cube.B0)];

                // Green
                case 1:
                    return moment[GetIndex(cube.R1, position, cube.B1)]
                       - moment[GetIndex(cube.R1, position, cube.B0)]
                       - moment[GetIndex(cube.R0, position, cube.B1)]
                       + moment[GetIndex(cube.R0, position, cube.B0)];

                // Blue
                case 0:
                    return moment[GetIndex(cube.R1, cube.G1, position)]
                       - moment[GetIndex(cube.R1, cube.G0, position)]
                       - moment[GetIndex(cube.R0, cube.G1, position)]
                       + moment[GetIndex(cube.R0, cube.G0, position)];

                default:
                    throw new ArgumentOutOfRangeException("direction");
            }
        }

        /// <summary>
        /// Clears the tables.
        /// </summary>
        private void Clear()
        {
            Array.Clear(vwt, 0, TableLength);
            Array.Clear(vmr, 0, TableLength);
            Array.Clear(vmg, 0, TableLength);
            Array.Clear(vmb, 0, TableLength);
            Array.Clear(m2, 0, TableLength);

            Array.Clear(tag, 0, TableLength);
        }

        /// <summary>
        /// Builds a 3-D color histogram of <c>counts, r/g/b, c^2</c>.
        /// </summary>
        /// <param name="image">The image.</param>
        private void Build3DHistogram(byte[] image)
        {
            for (int i = 0; i < image.Length; i += 4)
            {
                int r = image[i + 2];
                int g = image[i + 1];
                int b = image[i];

                int inr = r >> (8 - IndexBits);
                int ing = g >> (8 - IndexBits);
                int inb = b >> (8 - IndexBits);

                int ind = GetIndex(inr + 1, ing + 1, inb + 1);

                vwt[ind]++;
                vmr[ind] += r;
                vmg[ind] += g;
                vmb[ind] += b;
                m2[ind] += (r * r) + (g * g) + (b * b);
            }
        }

        /// <summary>
        /// Converts the histogram into moments so that we can rapidly calculate
        /// the sums of the above quantities over any desired box.
        /// </summary>
        private void Get3DMoments()
        {
            long[] area = new long[IndexCount];
            long[] areaR = new long[IndexCount];
            long[] areaG = new long[IndexCount];
            long[] areaB = new long[IndexCount];
            double[] area2 = new double[IndexCount];

            for (int r = 1; r < IndexCount; r++)
            {
                Array.Clear(area, 0, IndexCount);
                Array.Clear(areaR, 0, IndexCount);
                Array.Clear(areaG, 0, IndexCount);
                Array.Clear(areaB, 0, IndexCount);
                Array.Clear(area2, 0, IndexCount);

                for (int g = 1; g < IndexCount; g++)
                {
                    long line = 0;
                    long lineR = 0;
                    long lineG = 0;
                    long lineB = 0;
                    double line2 = 0;

                    for (int b = 1; b < IndexCount; b++)
                    {
                        int ind1 = GetIndex(r, g, b);

                        line += vwt[ind1];
                        lineR += vmr[ind1];
                        lineG += vmg[ind1];
                        lineB += vmb[ind1];
                        line2 += m2[ind1];

                        area[b] += line;
                        areaR[b] += lineR;
                        areaG[b] += lineG;
                        areaB[b] += lineB;
                        area2[b] += line2;

                        int ind2 = ind1 - GetIndex(1, 0, 0);

                        vwt[ind1] = vwt[ind2] + area[b];
                        vmr[ind1] = vmr[ind2] + areaR[b];
                        vmg[ind1] = vmg[ind2] + areaG[b];
                        vmb[ind1] = vmb[ind2] + areaB[b];
                        m2[ind1] = m2[ind2] + area2[b];
                    }
                }
            }
        }

        /// <summary>
        /// Computes the weighted variance of a box.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <returns>The result.</returns>
        private double Variance(ColorRange cube)
        {
            double dr = Volume(cube, vmr);
            double dg = Volume(cube, vmg);
            double db = Volume(cube, vmb);

            double xx = m2[GetIndex(cube.R1, cube.G1, cube.B1)]
             - m2[GetIndex(cube.R1, cube.G1, cube.B0)]
             - m2[GetIndex(cube.R1, cube.G0, cube.B1)]
             + m2[GetIndex(cube.R1, cube.G0, cube.B0)]
             - m2[GetIndex(cube.R0, cube.G1, cube.B1)]
             + m2[GetIndex(cube.R0, cube.G1, cube.B0)]
             + m2[GetIndex(cube.R0, cube.G0, cube.B1)]
             - m2[GetIndex(cube.R0, cube.G0, cube.B0)];

            return xx - (((dr * dr) + (dg * dg) + (db * db)) / Volume(cube, vwt));
        }

        /// <summary>
        /// We want to minimize the sum of the variances of two sub-boxes.
        /// The sum(c^2) terms can be ignored since their sum over both sub-boxes
        /// is the same (the sum for the whole box) no matter where we split.
        /// The remaining terms have a minus sign in the variance formula,
        /// so we drop the minus sign and maximize the sum of the two terms.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="first">The first position.</param>
        /// <param name="last">The last position.</param>
        /// <param name="cut">The cutting point.</param>
        /// <param name="wholeR">The whole red.</param>
        /// <param name="wholeG">The whole green.</param>
        /// <param name="wholeB">The whole blue.</param>
        /// <param name="wholeW">The whole weight.</param>
        /// <returns>The result.</returns>
        private double Maximize(ColorRange cube, int direction, int first, int last, out int cut, double wholeR, double wholeG, double wholeB, double wholeW)
        {
            long baseR = Bottom(cube, direction, vmr);
            long baseG = Bottom(cube, direction, vmg);
            long baseB = Bottom(cube, direction, vmb);
            long baseW = Bottom(cube, direction, vwt);

            double max = 0.0;
            cut = -1;

            for (int i = first; i < last; i++)
            {
                double halfR = baseR + Top(cube, direction, i, vmr);
                double halfG = baseG + Top(cube, direction, i, vmg);
                double halfB = baseB + Top(cube, direction, i, vmb);
                double halfW = baseW + Top(cube, direction, i, vwt);

                if (halfW == 0)
                {
                    continue;
                }

                double temp = ((halfR * halfR) + (halfG * halfG) + (halfB * halfB)) / halfW;

                halfR = wholeR - halfR;
                halfG = wholeG - halfG;
                halfB = wholeB - halfB;
                halfW = wholeW - halfW;

                if (halfW == 0)
                {
                    continue;
                }

                temp += ((halfR * halfR) + (halfG * halfG) + (halfB * halfB)) / halfW;

                if (temp > max)
                {
                    max = temp;
                    cut = i;
                }
            }

            return max;
        }

        /// <summary>
        /// Cuts a box.
        /// </summary>
        /// <param name="set1">The first set.</param>
        /// <param name="set2">The second set.</param>
        /// <returns>Returns a value indicating whether the box has been split.</returns>
        private bool Cut(ColorRange set1, ColorRange set2)
        {
            double wholeR = Volume(set1, vmr);
            double wholeG = Volume(set1, vmg);
            double wholeB = Volume(set1, vmb);
            double wholeW = Volume(set1, vwt);

            int cutr;
            int cutg;
            int cutb;

            double maxr = Maximize(set1, 2, set1.R0 + 1, set1.R1, out cutr, wholeR, wholeG, wholeB, wholeW);
            double maxg = Maximize(set1, 1, set1.G0 + 1, set1.G1, out cutg, wholeR, wholeG, wholeB, wholeW);
            double maxb = Maximize(set1, 0, set1.B0 + 1, set1.B1, out cutb, wholeR, wholeG, wholeB, wholeW);

            int dir;

            if ((maxr >= maxg) && (maxr >= maxb))
            {
                dir = 2;

                if (cutr < 0)
                {
                    return false;
                }
            }
            else if ((maxg >= maxr) && (maxg >= maxb))
            {
                dir = 1;
            }
            else
            {
                dir = 0;
            }

            set2.R1 = set1.R1;
            set2.G1 = set1.G1;
            set2.B1 = set1.B1;

            switch (dir)
            {
                // Red
                case 2:
                    set2.R0 = set1.R1 = cutr;
                    set2.G0 = set1.G0;
                    set2.B0 = set1.B0;
                    break;

                // Green
                case 1:
                    set2.G0 = set1.G1 = cutg;
                    set2.R0 = set1.R0;
                    set2.B0 = set1.B0;
                    break;

                // Blue
                case 0:
                    set2.B0 = set1.B1 = cutb;
                    set2.R0 = set1.R0;
                    set2.G0 = set1.G0;
                    break;
            }

            set1.Volume = (set1.R1 - set1.R0) * (set1.G1 - set1.G0) * (set1.B1 - set1.B0);
            set2.Volume = (set2.R1 - set2.R0) * (set2.G1 - set2.G0) * (set2.B1 - set2.B0);

            return true;
        }

        /// <summary>
        /// Marks a color space tag.
        /// </summary>
        /// <param name="cube">The cube.</param>
        /// <param name="label">A label.</param>
        private void Mark(ColorRange cube, byte label)
        {
            for (int r = cube.R0 + 1; r <= cube.R1; r++)
            {
                for (int g = cube.G0 + 1; g <= cube.G1; g++)
                {
                    for (int b = cube.B0 + 1; b <= cube.B1; b++)
                    {
                        tag[GetIndex(r, g, b)] = label;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the cube.
        /// </summary>
        /// <param name="colorCount">The color count.</param>
        private ColorRange[] BuildCube(ref int colorCount)
        {
            var cube = new ColorRange[colorCount];
            double[] vv = new double[colorCount];

            for (int i = 0; i < colorCount; i++)
            {
                cube[i] = new ColorRange();
            }

            cube[0].R0 = cube[0].G0 = cube[0].B0 = 0;
            cube[0].R1 = cube[0].G1 = cube[0].B1 = IndexCount - 1;

            int next = 0;

            for (int i = 1; i < colorCount; i++)
            {
                if (Cut(cube[next], cube[i]))
                {
                    vv[next] = cube[next].Volume > 1 ? Variance(cube[next]) : 0.0;
                    vv[i] = cube[i].Volume > 1 ? Variance(cube[i]) : 0.0;
                }
                else
                {
                    vv[next] = 0.0;
                    i--;
                }

                next = 0;

                double temp = vv[0];
                for (int k = 1; k <= i; k++)
                {
                    if (vv[k] > temp)
                    {
                        temp = vv[k];
                        next = k;
                    }
                }

                if (temp <= 0.0)
                {
                    colorCount = i + 1;
                    break;
                }
            }

            return cube;
        }

        /// <summary>
        /// Generates the quantized result.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="colorCount">The color count.</param>
        /// <param name="palette">The cube.</param>
        /// <returns>The result.</returns>
        private ColorQuantizerResult GenerateResult(byte[] image, int colorCount, ColorRange[] cube)
        {
            var quantizedImage = new ColorQuantizerResult(image.Length / 4);
            var palette = new Color[colorCount];
            for (int k = 0; k < colorCount; k++)
            {
                Mark(cube[k], (byte)k);

                double weight = Volume(cube[k], vwt);
                byte r = 0;
                byte g = 0;
                byte b = 0;
                if (weight != 0)
                {
                    r = (byte)(Volume(cube[k], vmr) / weight);
                    g = (byte)(Volume(cube[k], vmg) / weight);
                    b = (byte)(Volume(cube[k], vmb) / weight);
                }
                palette[k] = Color.FromArgb(r, g, b);
            }

            for (int i = 0; i < image.Length / 4; i++)
            {
                int r = image[(i * 4) + 2] >> (8 - IndexBits);
                int g = image[(i * 4) + 1] >> (8 - IndexBits);
                int b = image[i * 4] >> (8 - IndexBits);

                int ind = GetIndex(r + 1, g + 1, b + 1);
                byte label = tag[ind];
                var color = palette[label];
                quantizedImage.IncrementColor(color);
            }

            return quantizedImage;
        }
    }
}

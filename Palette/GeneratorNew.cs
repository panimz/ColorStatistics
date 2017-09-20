using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using PixelParser.Converters.Formats;
using PixelParser.Converters;

namespace PixelParser.Palette
{
    // port chroma.palette-gen.js certed by 2016  Mathieu Jacomy
    // https://github.com/medialab/iwanthue/blob/master/js/libs/chroma.palette-gen.js

    public class GeneratorNew
    {
        private Dictionary<string, LabColor> simulateCache = new Dictionary<string, LabColor>();

        public Color[] Generate(
            int colorsCount = 8,
            Predicate<LabColor> checkColor = null,
            bool forceMode = false,
            int quality = 50,
            bool ultraPrecision = false,
            ColorDistanceType distanceType = ColorDistanceType.Default)
        {
            if (checkColor == null)
            {
                checkColor = (lab) => { return true; };
            }
            if (forceMode)
            {
                return GenerateForceVectorMode(colorsCount, checkColor, quality, ultraPrecision, distanceType);
            }
            else
            {
                return GenerateKMeanMode(colorsCount, checkColor, quality, ultraPrecision, distanceType);
            }
        }

        private Color[] GenerateKMeanMode(int colorsCount,
            Predicate<LabColor> checkColor,
            int quality, bool ultraPrecision, 
            ColorDistanceType distanceType)
        {
            var kMeans = new List<LabColor>();
            for (var i = 0; i < colorsCount; i++)
            {
                var lab = LabColor.GetRandom();
                while (!ValidateLab(lab))
                {
                    lab = LabColor.GetRandom();
                }
                kMeans.Add(lab);
            }

            var lStep = ultraPrecision ? 1 : 5;
            var aStep = ultraPrecision ? 5 : 10;
            var bStep = ultraPrecision ? 5 : 10;

            var colorSamples = GenerateColorSamples(checkColor, lStep, aStep, bStep);

            // Steps
            var steps = quality;
            var samplesCount = colorSamples.Count;
            var samplesClosest = new int?[samplesCount];
            while (steps-- > 0)
            {
                // kMeans -> Samples Closest
                for (var i = 0; i < samplesCount; i++)
                {
                    var lab = colorSamples[i];
                    var minDistance = double.MaxValue;
                    for (var j = 0; j < kMeans.Count; j++)
                    {
                        var kMean = kMeans[j];
                        var distance = GetColorDistance(lab, kMean, distanceType);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            samplesClosest[i] = j;
                        }
                    }
                }
                // Samples -> kMeans
                var freeColorSamples = colorSamples
                    .Select((lab) => (LabColor)lab.Clone()) // todo do I really need clone each item?
                    .ToList();
                for (var j = 0; j < kMeans.Count; j++)
                {
                    var count = 0;
                    var candidateKMean = new LabColor();
                    for (var i = 0; i < colorSamples.Count; i++)
                    {
                        if (samplesClosest[i] == j)
                        {
                            count++;
                            var color = colorSamples[i];
                            candidateKMean.L += color.L;
                            candidateKMean.A += color.A;
                            candidateKMean.B += color.B;
                        }
                    }
                    if (count != 0)
                    {
                        candidateKMean.L /= count;
                        candidateKMean.A /= count;
                        candidateKMean.B /= count;
                    }

                    if (count != 0 && ValidateLab(candidateKMean) && checkColor(candidateKMean))
                    {
                        kMeans[j] = candidateKMean;
                    }
                    else
                    {
                        // The candidate kMean is out of the boundaries of the color space, or unfound.
                        if (freeColorSamples.Count > 0)
                        {
                            // We just search for the closest FREE color of the candidate kMean
                            var minDistance = double.MaxValue;
                            var closest = -1;
                            for (var i = 0; i < freeColorSamples.Count; i++)
                            {
                                var distance = GetColorDistance(freeColorSamples[i], candidateKMean, distanceType);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    closest = i;
                                }
                            }
                            kMeans[j] = colorSamples[closest];
                        }
                        else
                        {
                            // Then we just search for the closest color of the candidate kMean
                            var minDistance = double.MaxValue;
                            var closest = -1;
                            for (var i = 0; i < colorSamples.Count; i++)
                            {
                                var distance = GetColorDistance(colorSamples[i], candidateKMean, distanceType);
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    closest = i;
                                }
                            }
                            kMeans[j] = colorSamples[closest];
                        }
                    }
                    var baseColor = kMeans[j];
                    freeColorSamples = freeColorSamples
                        .Where((lab) => !lab.Equals(baseColor))
                        .ToList();
                }
            }
            return kMeans.Select((lab) => lab.ToRgb()).ToArray();
        }

        private List<LabColor> GenerateColorSamples(Predicate<LabColor> checkColor, 
            int lStep, int aStep, int bStep)
        {
            var colorSamples = new List<LabColor>();
            for (var l = 0; l <= 100; l += lStep)
            {
                for (var a = -100; a <= 100; a += aStep)
                {
                    for (var b = -100; b <= 100; b += bStep)
                    {
                        var color = new LabColor(l, a, b);
                        if (ValidateLab(color) && checkColor(color))
                        {
                            colorSamples.Add(color);
                        }
                    }
                }
            }
            return colorSamples;
        }

        private Color[] GenerateForceVectorMode(int colorsCount,
            Predicate<LabColor> checkColor,
            int quality,
            bool ultraPrecision,
            ColorDistanceType distanceType)
        {
            var random = new Random();
            var colors = new LabColor[colorsCount];

            // Init
            for (var i = 0; i < colorsCount; i++)
            {
                // Find a valid Lab color
                var color = LabColor.GetRandom();
                while (!ValidateLab(color) || !checkColor(color))
                {
                    color = LabColor.GetRandom();
                }
                colors[i] = color;
            }

            // Force vector: repulsion
            var repulsion = 100;
            var speed = 100;
            var steps = quality * 20;
            var vectors = new LabVector[colorsCount];
            while (steps-- > 0)
            {
                // Init
                for (var i = 0; i < colors.Length; i++)
                {
                    vectors[i] = new LabVector();
                }
                // Compute Force
                for (var i = 0; i < colors.Length; i++)
                {
                    var colorA = colors[i];
                    for (var j = 0; j < i; j++)
                    {
                        var colorB = colors[j];

                        // repulsion force
                        var dl = colorA.L - colorB.L;
                        var da = colorA.A - colorB.A;
                        var db = colorA.B - colorB.B;
                        var d = GetColorDistance(colorA, colorB, distanceType);
                        if (d > 0)
                        {
                            var force = repulsion / Math.Pow(d, 2);

                            vectors[i].dL += dl * force / d;
                            vectors[i].dA += da * force / d;
                            vectors[i].dB += db * force / d;

                            vectors[j].dL -= dl * force / d;
                            vectors[j].dA -= da * force / d;
                            vectors[j].dB -= db * force / d;
                        }
                        else
                        {
                            // Jitter
                            vectors[j].dL += 2 - 4 * random.NextDouble();
                            vectors[j].dA += 2 - 4 * random.NextDouble();
                            vectors[j].dB += 2 - 4 * random.NextDouble();
                        }
                    }
                }
                // Apply Force
                for (var i = 0; i < colors.Length; i++)
                {
                    var color = colors[i];
                    var displacement = speed * vectors[i].Magnitude; ;
                    if (displacement > 0)
                    {
                        var ratio = speed * Math.Min(0.1, displacement) / displacement;
                        var l = color.L + vectors[i].dL * ratio;
                        var a = color.A + vectors[i].dA * ratio;
                        var b = color.B + vectors[i].dB * ratio;
                        var candidateLab = new LabColor(l, a, b);
                        if (ValidateLab(candidateLab) && checkColor(candidateLab))
                        {
                            colors[i] = candidateLab;
                        }
                    }
                }
            }

            return colors.Select((lab) => lab.ToRgb()).ToArray();
        }

        private double GetColorDistance(
            LabColor lab1,
            LabColor lab2,
            ColorDistanceType type = ColorDistanceType.Default)
        {

            switch (type)
            {
                case ColorDistanceType.Default:
                case ColorDistanceType.Euclidian:
                    return EuclidianDistance(lab1, lab2);
                case ColorDistanceType.CMC:
                    return CmcDistance(lab1, lab2);
                case ColorDistanceType.Compromise:
                    return CompromiseDistance(lab1, lab2);
                default:
                    return DistanceColorBlind(lab1, lab2, type);
            }
        }

        private double EuclidianDistance(LabColor lab1, LabColor lab2)
        {
            var dL = lab1.L - lab2.L;
            var dA = lab1.A - lab2.A;
            var dB = lab1.B - lab2.B;
            return Math.Sqrt(dL * dL + dA * dA + dB * dB);
        }

        private double DistanceColorBlind(LabColor lab1,
            LabColor lab2,
            ColorDistanceType type)
        {
            var lab1Cb = Simulate(lab1, type);
            var lab2Cb = Simulate(lab2, type);
            return CmcDistance(lab1Cb, lab2Cb);
        }

        // http://www.brucelindbloom.com/index.html?Eqn_DeltaE_CMC.html
        private double CmcDistance(LabColor lab1, LabColor lab2, int l = 2, int c = 1)
        {
            var L1 = lab1.L;
            var L2 = lab2.L;
            var a1 = lab1.A;
            var a2 = lab2.A;
            var b1 = lab1.B;
            var b2 = lab2.B;
            var C1 = Math.Sqrt(a1 * a1 + b1 * b1);
            var C2 = Math.Sqrt(a2 * a2 + b2 * b2);
            var deltaC = C1 - C2;
            var deltaL = L1 - L2;
            var deltaA = a1 - a2;
            var deltaB = b1 - b2;
            var deltaH = Math.Sqrt(Math.Pow(deltaA, 2) + Math.Pow(deltaB, 2) + Math.Pow(deltaC, 2));
            var H1 = Math.Atan2(b1, a1) * (180 / Math.PI);
            while (H1 < 0) { H1 += 360; }
            var F = Math.Sqrt(Math.Pow(C1, 4) / (Math.Pow(C1, 4) + 1900));
            var T = (164 <= H1 && H1 <= 345) ?
                (0.56 + Math.Abs(0.2 * Math.Cos(H1 + 168))) :
                (0.36 + Math.Abs(0.4 * Math.Cos(H1 + 35)));
            var S_L = (lab1.L < 16) ? (0.511) : (0.040975 * L1 / (1 + 0.01765 * L1));
            var S_C = (0.0638 * C1 / (1 + 0.0131 * C1)) + 0.638;
            var S_H = S_C * (F * T + 1 - F);
            var result = Math.Sqrt(Math.Pow(deltaL / (l * S_L), 2) + Math.Pow(deltaC / (c * S_C), 2) + Math.Pow(deltaH / S_H, 2));
            return result;
        }

        // WARNING: may return [NaN, NaN, NaN]
        private LabColor Simulate(LabColor lab, ColorDistanceType type, int amount = 1)
        {

            // Cache
            var key = string.Format("{0}-{1}{2}", lab, type, amount);
            if (this.simulateCache.ContainsKey(key))
            {
                return this.simulateCache[key];
            }

            // Get data from type
            var confuse = ConfusionLine.Get(type);

            // Code adapted from http://galacticmilk.com/labs/Color-Vision/Javascript/Color.Vision.Simulate.js
            var color = lab.ToRgb();
            var sr = color.R;
            var sg = color.G;
            var sb = color.B;
            double dr = sr; // destination color
            double dg = sg;
            double db = sb;
            // Convert source color into XYZ color space
            var pow_r = Math.Pow(sr, 2.2);
            var pow_g = Math.Pow(sg, 2.2);
            var pow_b = Math.Pow(sb, 2.2);
            var X = pow_r * 0.412424 + pow_g * 0.357579 + pow_b * 0.180464; // RGB->XYZ (sRGB:D65)
            var Y = pow_r * 0.212656 + pow_g * 0.715158 + pow_b * 0.0721856;
            var Z = pow_r * 0.0193324 + pow_g * 0.119193 + pow_b * 0.950444;
            // Convert XYZ into xyY Chromacity Coordinates (xy) and Luminance (Y)
            var chroma_x = X / (X + Y + Z);
            var chroma_y = Y / (X + Y + Z);
            // Generate the "Confusion Line" between the source color and the Confusion Point
            var m = (chroma_y - confuse.Y) / (chroma_x - confuse.X); // slope of Confusion Line
            var yint = chroma_y - chroma_x * m; // y-intercept of confusion line (x-intercept = 0.0)
                                                // How far the xy coords deviate from the simulation
            var deviate_x = (confuse.Yint - yint) / (m - confuse.M);
            var deviate_y = (m * deviate_x) + yint;
            // Compute the simulated color's XYZ coords
            X = deviate_x * Y / deviate_y;
            Z = (1.0 - (deviate_x + deviate_y)) * Y / deviate_y;
            // Neutral grey calculated from luminance (in D65)
            var neutral_X = 0.312713 * Y / 0.329016;
            var neutral_Z = 0.358271 * Y / 0.329016;
            // Difference between simulated color and neutral grey
            var diff_X = neutral_X - X;
            var diff_Z = neutral_Z - Z;
            var diff_r = diff_X * 3.24071 + diff_Z * -0.498571; // XYZ->RGB (sRGB:D65)
            var diff_g = diff_X * -0.969258 + diff_Z * 0.0415557;
            var diff_b = diff_X * 0.0556352 + diff_Z * 1.05707;
            // Convert to RGB color space
            dr = X * 3.24071 + Y * -1.53726 + Z * -0.498571; // XYZ->RGB (sRGB:D65)
            dg = X * -0.969258 + Y * 1.87599 + Z * 0.0415557;
            db = X * 0.0556352 + Y * -0.203996 + Z * 1.05707;
            // Compensate simulated color towards a neutral fit in RGB space
            var fit_r = ((dr < 0.0 ? 0.0 : 1.0) - dr) / diff_r;
            var fit_g = ((dg < 0.0 ? 0.0 : 1.0) - dg) / diff_g;
            var fit_b = ((db < 0.0 ? 0.0 : 1.0) - db) / diff_b;
            var adjust = Math.Max( // highest value
                Math.Max(
                    (fit_r > 1.0 || fit_r < 0.0) ? 0.0 : fit_r,
                    (fit_g > 1.0 || fit_g < 0.0) ? 0.0 : fit_g),
                (fit_b > 1.0 || fit_b < 0.0) ? 0.0 : fit_b
            );
            // Shift proportional to the greatest shift
            dr = dr + (adjust * diff_r);
            dg = dg + (adjust * diff_g);
            db = db + (adjust * diff_b);
            // Apply gamma correction
            dr = Math.Pow(dr, 1.0 / 2.2);
            dg = Math.Pow(dg, 1.0 / 2.2);
            db = Math.Pow(db, 1.0 / 2.2);
            // Anomylize colors
            dr = sr * (1.0 - amount) + dr * amount;
            dg = sg * (1.0 - amount) + dg * amount;
            db = sb * (1.0 - amount) + db * amount;
            var dcolor = Color.FromArgb(
                (int)Math.Round(dr),
                (int)Math.Round(dg),
                (int)Math.Round(db));
            var result = dcolor.ToLab();
            this.simulateCache[key] = result;
            return result;
        }

        private double CompromiseDistance(LabColor lab1, LabColor lab2)
        {
            var distances = new List<double>() { CmcDistance(lab1, lab2) };
            var coeffs = new List<int>() { 1000 };
            var types = new Dictionary<ColorDistanceType, int> {
                                { ColorDistanceType.Protanope, 100 },
                                { ColorDistanceType.Deuteranope, 500},
                                { ColorDistanceType.Tritanope, 1}
                            };
            foreach (var type in types)
            {
                var lab1Cb = Simulate(lab1, type.Key);
                var lab2Cb = Simulate(lab2, type.Key);
                if (!(lab1Cb.HasNaN() || lab2Cb.HasNaN()))
                {
                    distances.Add(CmcDistance(lab1Cb, lab2Cb));
                    coeffs.Add(type.Value);
                }
            }
            var total = 0.0;
            var count = 0.0;
            for (var i = 0; i < distances.Count; i++)
            {
                total += coeffs[i] * distances[i];
                count += coeffs[i];
            }
            return total / count;
        }

        /// <summary>
        ///  check if a Lab color exists in the rgb space
        /// </summary>
        /// <param name="lab"></param>
        /// <returns></returns>
        private bool ValidateLab(LabColor lab)
        {
            var y = (lab.L + 16) / 116;
            var x = (double.IsNaN(lab.A)) ? (y) : (y + lab.A / 500);
            var z = (double.IsNaN(lab.B)) ? (y) : (y - lab.B / 200);

            y = LabConstants.Yn * lab2xyz(y);
            x = LabConstants.Xn * lab2xyz(x);
            z = LabConstants.Zn * lab2xyz(z);

            var r = xyz2rgb(3.2404542 * x - 1.5371385 * y - 0.4985314 * z);  // D65 -> sRGB
            var g = xyz2rgb(-0.9692660 * x + 1.8760108 * y + 0.0415560 * z);
            var b = xyz2rgb(0.0556434 * x - 0.2040259 * y + 1.0572252 * z);

            return r >= 0 && r <= 255
                 && g >= 0 && g <= 255
                 && b >= 0 && b <= 255;
        }

        private int xyz2rgb(double channel)
        {
            var data = (channel <= 0.00304) ?
                (12.92 * channel) :
                (1.055 * Math.Pow(channel, 1.0 / 2.4) - 0.055);
            return (int)Math.Round(255 * data);
        }

        private double lab2xyz(double t)
        {
            return (t > LabConstants.t1) ?
                (t * t * t) :
                (LabConstants.t2 * (t - LabConstants.t0));
        }
    }
}

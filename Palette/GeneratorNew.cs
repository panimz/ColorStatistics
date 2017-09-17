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

    public static class LAB_CONSTANTS
    {
        // Corresponds roughly to RGB brighter/darker
        public static int Kn = 18;

        // D65 standard referent
        public static double Xn = 0.950470;
        public static double Yn = 1;
        public static double Zn = 1.088830;

        public static double t0 = 0.137931034; // 4 / 29
        public static double t1 = 0.206896552;  // 6 / 29
        public static double t2 = 0.12841855;   // 3 * t1 * t1
        public static double t3 = 0.008856452;  // t1 * t1 * t1
    }

    enum ColorDistance
    {
        Default,
        Euclidian,
        CMC,
        Compromise,
        Protanope,
        Deuteranope,
        Tritanope
    }

    class LabVector
    {
        public LabVector()
        {
            dL = 0.0;
            dA = 0.0;
            dB = 0.0;
        }

        public LabVector(double dl, double da, double db)
        {
            dL = dl;
            dA = da;
            dB = db;
        }

        public double dL { get; set; }
        public double dA { get; set; }
        public double dB { get; set; }
        public double Magnitude
        {
            get { return Math.Sqrt(dL * dL + dB * dB + dA * dA); }
        }
    }

    class ConfusionLine
    {
        public double X;
        public double Y;
        public double M;
        public double Yint;
    }

    class GeneratorNew
    {
        private Dictionary<string, LabColor> simulateCache = new Dictionary<string, LabColor>();

        private static Dictionary<ColorDistance, ConfusionLine> confusionLines = new Dictionary<ColorDistance, ConfusionLine>() {
            {
                ColorDistance.Protanope,
                new ConfusionLine() {
                    X = 0.7465,
                    Y = 0.2535,
                    M = 1.273463,
                    Yint = -0.073894
                }
            },
            {
                ColorDistance.Deuteranope,
                new ConfusionLine() {
                    X = 1.4,
                    Y = -0.4,
                    M = 0.968437,
                    Yint = 0.003331
                }
            },
            {
                ColorDistance.Tritanope,
                new ConfusionLine() {
                    X = 0.1748,
                    Y = 0.0,
                    M = 0.062921,
                    Yint = 0.292119
                }
            }
        };

        public List<Color> Generate(
            int colorsCount = 8,
            Predicate<object> checkColor,
            bool forceMode = false,
            int quality = 50,
            bool ultraPrecision = false,
            ColorDistance distanceType = ColorDistance.Default)
        {
            if (forceMode)
            {
                return GenerateForceVectorMode(colorsCount, checkColor, quality, ultraPrecision, distanceType);
            }
            else
            {
                return GenerateKMeanMode(colorsCount, checkColor, quality, ultraPrecision, distanceType);
            }
        }

        private List<Color> GenerateKMeanMode(int colorsCount, Predicate<object> checkColor, int quality, bool ultraPrecision, ColorDistance distanceType)
        {
            throw new NotImplementedException();
        }

        private List<Color> GenerateForceVectorMode(int colorsCount, Predicate<object> checkColor, int quality, bool ultraPrecision, ColorDistance distanceType)
        {
            var random = new Random();
            var colors = new List<LabColor>();

            // Init
            for (var i = 0; i < colorsCount; i++)
            {
                // Find a valid Lab color
                var l = 100 * random.NextDouble();
                var a = 100 * (2 * random.NextDouble() - 1);
                var b = 100 * (2 * random.NextDouble() - 1);
                var color = new LabColor(l, a, b);
                while (!ValidateLab(color) || !checkColor(color))
                {
                    l = 100 * random.NextDouble();
                    a = 100 * (2 * random.NextDouble() - 1);
                    b = 100 * (2 * random.NextDouble() - 1);
                    color = new LabColor(l, a, b);
                }
                colors.Add(color);
            }

            // Force vector: repulsion
            var repulsion = 100;
            var speed = 100;
            var steps = quality * 20;
            var vectors = new List<LabVector>();
            while (steps-- > 0)
            {
                // Init
                for (var i = 0; i < colors.Count; i++)
                {
                    vectors[i] = new LabVector();
                }
                // Compute Force
                for (var i = 0; i < colors.Count; i++)
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
                for (var i = 0; i < colors.Count; i++)
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
                        if (CheckLab(candidateLab))
                        {
                            colors[i] = candidateLab;
                        }
                    }
                }
            }

            return colors.Select((lab) => lab.ToRgb()).ToList();
        }

        private double GetColorDistance(
            LabColor lab1,
            LabColor lab2,
            ColorDistance type = ColorDistance.Default)
        {

            switch (type)
            {
                case ColorDistance.Default:
                case ColorDistance.Euclidian:
                    return EuclidianDistance(lab1, lab2);
                case ColorDistance.CMC:
                    return CmcDistance(lab1, lab2);
                case ColorDistance.Compromise:
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
            ColorDistance type)
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
        private LabColor Simulate(LabColor lab, ColorDistance type, int amount = 1)
        {

            // Cache
            var key = string.Format("{0}-{1}{2}", lab, type, amount);
            if (this.simulateCache.ContainsKey(key))
            {
                return this.simulateCache[key];
            }

            // Get data from type
            var confuse_x = confusionLines[type].X;
            var confuse_y = confusionLines[type].Y;
            var confuse_m = confusionLines[type].M;
            var confuse_yint = confusionLines[type].Yint;

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
            var m = (chroma_y - confuse_y) / (chroma_x - confuse_x); // slope of Confusion Line
            var yint = chroma_y - chroma_x * m; // y-intercept of confusion line (x-intercept = 0.0)
                                                // How far the xy coords deviate from the simulation
            var deviate_x = (confuse_yint - yint) / (m - confuse_m);
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
                (fit_r > 1.0 || fit_r < 0.0) ? 0.0 : fit_r,
                (fit_g > 1.0 || fit_g < 0.0) ? 0.0 : fit_g,
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
            var types = new Dictionary<ColorDistance, int> {
                                { ColorDistance.Protanope, 100 },
                                { ColorDistance.Deuteranope, 500},
                                { ColorDistance.Tritanope, 1}
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

        private bool CheckLab(LabColor lab)
        {
            return ValidateLab(lab);// && CheckColor(lab.ToRgb());
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

            y = LAB_CONSTANTS.Yn * lab2xyz(y);
            x = LAB_CONSTANTS.Xn * lab2xyz(x);
            z = LAB_CONSTANTS.Zn * lab2xyz(z);

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
            return (t > LAB_CONSTANTS.t1) ?
                (t * t * t) :
                (LAB_CONSTANTS.t2 * (t - LAB_CONSTANTS.t0));
        }
    }
}

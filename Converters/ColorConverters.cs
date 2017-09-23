using System;
using System.Drawing;
using PixelParser.Converters.Formats;

namespace PixelParser.Converters
{
    public static class ColorConverters
    {
        public static HsvColor ToHsv(this Color color)
        {
            var r = color.R;
            var g = color.G;
            var b = color.B;
            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));
            var delta = max - min;
            var v = max / 255.0f;
            var s = 0;
            var h = double.NaN;
            if (max != 0f)
            {
                s = delta / max;
                if (r == max)
                {
                    h = (g - b) / delta;
                }
                else if (g == max)
                {
                    h = 2 + (b - r) / delta;
                }
                if (b == max)
                {
                    h = 4 + (r - g) / delta;
                }
                h *= 60;
                if (h < 0)
                {
                    h += 360;
                }
            }
            return new HsvColor(h, s, v);
        }

        public static HslColor ToHsl(this Color color)
        {
            var r = color.R / 255f;
            var g = color.G / 255f;
            var b = color.B / 255f;

            var min = Math.Min(r, Math.Min(g, b));
            var max = Math.Max(r, Math.Max(g, b));

            var l = (max + min) / 2f;
            var s = 0f;
            if (max > min)
            {
                if (l < 0.5)
                {
                    s = (max - min) / (max + min);
                }
                else
                {
                    s = (max - min) / (2 - max - min);
                }
            }
            var h = double.NaN;
            if (r == max)
            {
                h = (g - b) / (max - min);
            }
            else if (g == max)
            {
                h = 2 + (b - r) / (max - min);
            }
            else
            {
                h = 4 + (r - g) / (max - min);
            }
            h *= 60;
            if (h < 0) { h += 360; }
            return new HslColor(h, s, l);
        }

        public static HclColor ToHcl(this Color color)
        {
            var lab = color.ToLab();
            return lab.ToHcl();
        }

        public static LabColor ToLab(this Color color)
        {
            var xyz = color.ToXyz();
            return xyz.ToLab();
        }

        public static XyzColor ToXyz(this Color color)
        {
            var r = ToXyzChannel(color.R);
            var g = ToXyzChannel(color.G);
            var b = ToXyzChannel(color.B);

            // Observer. = 2°, Illuminant = D65
            var x = r * 0.4124 + g * 0.3576 + b * 0.1805;
            var y = r * 0.2126 + g * 0.7152 + b * 0.0722;
            var z = r * 0.0193 + g * 0.1192 + b * 0.9505;

            return new XyzColor(x, y, z);
        }

        private static double ToXyzChannel(byte channel)
        {
            var c = channel / 255.0;
            var factor = (c > 0.04045) ?
                Math.Pow((c + 0.055) / 1.055, 2.4) :
                (c / 12.92);
            return factor;
        }
    }
}

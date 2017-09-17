using System;
using System.Drawing;
using PixelParser.Converters.Formats;

namespace PixelParser.Converters
{
    public static class ColorConverters
    {
        public static LabColor ToLab(this Color color)
        {
            var red = ToXyzChannel(color.R);
            var green = ToXyzChannel(color.G);
            var blue = ToXyzChannel(color.B);

            var x = ToLabChannel(0.4124564 * red + 0.3575761 * green + 0.1804375 * blue) / LabConstants.Xn;
            var y = ToLabChannel(0.2126729 * red + 0.7151522 * green + 0.0721750 * blue) / LabConstants.Yn;
            var z = ToLabChannel(0.0193339 * red + 0.1191920 * green + 0.9503041 * blue) / LabConstants.Zn;

            var l = 116.0 * y - 16.0;
            var a = 500.0 * (x - y);
            var b = 200.0 * (y - z);

            return new LabColor(l, a, b);
        }

        private static double ToXyzChannel(byte channel)
        {
            var c = channel / 255.0;
            return (c < 0.04045) ?
                (c / 12.92) :
                Math.Pow((c + 0.055) / 1.055f, 2.4);
        }

        private static double ToLabChannel(double channel)
        {
            return (channel > LabConstants.t3) ?
                Math.Pow(channel, 1 / 3.0) :
                (channel / LabConstants.t2 + LabConstants.t0);
        }

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
    }
}

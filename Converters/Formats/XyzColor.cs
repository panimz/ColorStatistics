using System;
using System.Drawing;

namespace PixelParser.Converters.Formats
{
    [Serializable]
    public struct XyzColor: ICloneable
    {
        public XyzColor(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X;
        public double Y;
        public double Z;

        public LabColor ToLab()
        {
            var x = ToLabChannel(this.X / (LabConstants.Xn));
            var y = ToLabChannel(this.Y / (LabConstants.Yn));
            var z = ToLabChannel(this.Z / (LabConstants.Zn));

            var l = Math.Max(0, 116 * y - 16);
            var a = 500 * (x - y);
            var b = 200 * (y - z);

            return new LabColor(l, a, b);
        }

        public Color ToRgb()
        {
            var r = ConvertXyzToRgb(3.2404542 * X - 1.5371385 * Y - 0.4985314 * Z);
            var g = ConvertXyzToRgb(-0.9692660 * X + 1.8760108 * Y + 0.0415560 * Z);
            var b = ConvertXyzToRgb(0.0556434 * X- 0.2040259 * Y + 1.0572252 * Z);

            return Color.FromArgb(ClampChannel(r), ClampChannel(g), ClampChannel(b));
        }

        public bool IsValidRgb()
        {
            var r = ConvertXyzToRgb(3.2404542 * X - 1.5371385 * Y - 0.4985314 * Z);
            if (!IsValidChannel(r))
            {
                return false;
            }
            var g = ConvertXyzToRgb(-0.9692660 * X + 1.8760108 * Y + 0.0415560 * Z);
            if (!IsValidChannel(g))
            {
                return false;
            }
            var b = ConvertXyzToRgb(0.0556434 * X - 0.2040259 * Y + 1.0572252 * Z);
            if (!IsValidChannel(b))
            {
                return false;
            }
            return true;
        }

        private static bool IsValidChannel(int channel)
        {
            return !(channel < 0 || channel > 255);
        }

        private static double ToLabChannel(double channel)
        {
            return (channel > LabConstants.t3) ?
                Math.Pow(channel, 1 / 3.0) :
                (channel / LabConstants.t2 + LabConstants.t0);
        }

        private static int ConvertXyzToRgb(double r)
        {
            var factor = (r <= 0.0031308) ?
                (12.92 * r) :
                (1.055 * Math.Pow(r, 1 / 2.4) - 0.055);
            return (int)Math.Round(255.0 * factor);
        }

        private static int ClampChannel(int channel)
        {
            if (channel < 0) { return 0; }
            if (channel > 255) { return 255; }
            return channel;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[X = {0}, Y = {1}, Z = {2}]", X, Y, Z);
        }

        internal bool HasNaN()
        {
            return double.IsNaN(X) ||
                double.IsNaN(Y) ||
                double.IsNaN(Z);
        }

        public object Clone()
        {
            return new XyzColor(X, Y, Z);
        }
    }
}

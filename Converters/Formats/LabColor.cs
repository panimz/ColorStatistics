using System;
using System.Drawing;

namespace PixelParser.Converters.Formats
{
    [Serializable]
    public struct LabColor: ICloneable
    {
        private static Random random = new Random();

        public static LabColor GetRandom() {
            var l = 100 * random.NextDouble();
            var a = 100 * (2 * random.NextDouble() - 1);
            var b = 100 * (2 * random.NextDouble() - 1);
            return new LabColor(l, a, b);
        }

        public LabColor(double l, double a, double b)
        {
            L = l;
            A = a;
            B = b;
        }

        public double L;
        public double A;
        public double B;

        public Color ToRgb()
        {
            var y = (L + 16) / 116;
            var x = double.IsNaN(A) ? y : (y + A / 500);
            var z = double.IsNaN(B) ? y : (y - B / 200);

            y = LabConstants.Yn * ConvertLabToXyz(y);
            x = LabConstants.Xn * ConvertLabToXyz(x);
            z = LabConstants.Zn * ConvertLabToXyz(z);

            var r = ConvertXyzToRgb(3.2404542 * x - 1.5371385 * y - 0.4985314 * z);
            var g = ConvertXyzToRgb(-0.9692660 * x + 1.8760108 * y + 0.0415560 * z);
            var b = ConvertXyzToRgb(0.0556434 * x - 0.2040259 * y + 1.0572252 * z);

            return Color.FromArgb(ClampChannel(r), ClampChannel(g), ClampChannel(b));
        }

        public HclColor ToHcl()
        {
            var c = Math.Sqrt(A * A + B * B);
            var h = (Math.Atan2(B, A) * (180 / Math.PI) + 360) % 360.0;
            if (Math.Round(c * 10000) == 0)
            {
                h = double.NaN;
            }
            return new HclColor(h, c, L);
        }

        private static double ConvertXyzToRgb(double r)
        {
            var factor = (r <= 0.00304) ?
                (12.92 * r) :
                (1.055 * Math.Pow(r, 1 / 2.4) - 0.055);
            return Math.Round(255 * factor);
        }

        private static double ConvertLabToXyz(double t)
        {
            return (t > LabConstants.t1) ?
                         (t * t * t) :
                         (LabConstants.t2 * (t - LabConstants.t0));
        }

        private static int ClampChannel(double channel)
        {
            if (channel < 0.0) { return 0; }
            if (channel > 255.0) { return 255; }
            return Convert.ToInt32(channel);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) { return false; }
            if (obj.GetType() != typeof(LabColor)) { return false; }
            var other = (LabColor)obj;
            return L == other.L && A == other.A && B == other.B;
        }

        public override int GetHashCode()
        {
            return L.GetHashCode() ^ A.GetHashCode() ^ B.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[L = {0}, A = {1}, B = {2}]", L, A, B);
        }

        internal bool HasNaN()
        {
            return double.IsNaN(L) ||
                double.IsNaN(A) ||
                double.IsNaN(B);
        }

        public object Clone()
        {
            return new LabColor(L, A, B);
        }
    }
}

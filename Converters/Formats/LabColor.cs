using System;
using System.Drawing;

namespace PixelParser.Converters.Formats
{
    [Serializable]
    public struct LabColor: ICloneable
    {
        private static Random random = new Random();

        public static LabColor GetRandom() {
            var l = random.Next(100);
            var a = random.Next(-128, 128);
            var b = random.Next(-128, 128);
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

        public Color ToRgb()
        {
            var xyz = this.ToXyz();
            return xyz.ToRgb();
        }

        public bool IsValidRgb()
        {
            var xyz = this.ToXyz();
            return xyz.IsValidRgb();
        }

        public XyzColor ToXyz()
        {
            var y = (L + 16.0) / 116.0;
            var x = A / 500.0 + y;
            var z = y - B / 200.0;

            var xyz = new XyzColor
            {
                X = LabConstants.Xn * ConvertLabToXyz(x),
                Y = LabConstants.Yn * ConvertLabToXyz(y),
                Z = LabConstants.Zn * ConvertLabToXyz(z),
            };
            return xyz;
        }

        private static double ConvertLabToXyz(double t)
        {
            return (t > LabConstants.t1) ?
                         (t * t * t) :
                         (LabConstants.t2 * (t - LabConstants.t0));
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

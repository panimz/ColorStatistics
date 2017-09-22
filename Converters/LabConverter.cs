using PixelParser.Converters.Formats;
using System;
using System.Drawing;

namespace PixelParser.Converters
{
    public static class LabConverter
    {
        public static LabColor ToColorSpace(Color color)
        {
            var xyz = XyzConverter.ToColorSpace(color);
            return LabConverter.FromXyz(xyz);
        }

        public static LabColor FromXyz(XyzColor xyz)
        {
            var white = XyzConverter.WhiteReference;
            var x = PivotXyz(xyz.X / white.X);
            var y = PivotXyz(xyz.Y / white.Y);
            var z = PivotXyz(xyz.Z / white.Z);

            var l = Math.Max(0, 116 * y - 16);
            var a = 500 * (x - y);
            var b = 200 * (y - z);

            return new LabColor(l, a, b);
        }

        public static Color ToColor(LabColor item)
        {
            var xyz = LabConverter.ToXyz(item);
            return XyzConverter.ToColor(xyz);
        }

        public static XyzColor ToXyz(LabColor item)
        {
            var y = (item.L + 16.0) / 116.0;
            var x = item.A / 500.0 + y;
            var z = y - item.B / 200.0;

            var white = XyzConverter.WhiteReference;
            var x3 = x * x * x;
            var z3 = z * z * z;
            var xyz = new XyzColor
            {
                X = white.X * (x3 > XyzConverter.Epsilon ? x3 : (x - 16.0 / 116.0) / 7.787),
                Y = white.Y * (item.L > (XyzConverter.Kappa * XyzConverter.Epsilon) ? Math.Pow(((item.L + 16.0) / 116.0), 3) : item.L / XyzConverter.Kappa),
                Z = white.Z * (z3 > XyzConverter.Epsilon ? z3 : (z - 16.0 / 116.0) / 7.787)
            };
            return xyz;
        }

        private static double PivotXyz(double n)
        {
            return n > XyzConverter.Epsilon ? CubicRoot(n) : (XyzConverter.Kappa * n + 16) / 116;
        }

        private static double CubicRoot(double n)
        {
            return Math.Pow(n, 1.0 / 3.0);
        }
    }
}

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

        public Color ToRgb()
        {
            var r = ToRgbChannel(3.2404542 * X - 1.5371385 * Y - 0.4985314 * Z);
            var g = ToRgbChannel(0.9692660 * X + 1.8760108 * Y + 0.0415560 * Z);
            var b = ToRgbChannel(0.0556434 * X - 0.2040259 * Y + 1.0572252 * Z);
            return Color.FromArgb(r, g, b);
        }

        private static int ToRgbChannel(double channel)
        {
            var c = (channel <= 0.00304) ?
                (12.92 * channel) :
                (1.055 * Math.Pow(channel, 1 / 2.4) - 0.055);
            return (int)Math.Round(255 * c);
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

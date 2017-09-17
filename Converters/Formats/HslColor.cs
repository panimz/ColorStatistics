using System;

namespace PixelParser.Converters
{
    [Serializable]
    public struct HslColor
    {
        public HslColor(double h, double s, double l)
        {
            H = h;
            S = s;
            L = l;
        }

        public double H;
        public double S;
        public double L;
    }
}

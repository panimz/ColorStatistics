using System;

namespace PixelParser.Converters.Formats
{
    [Serializable]
    public struct HsvColor
    {
        public HsvColor(double h, double s, double v)
        {
            H = h;
            S = s;
            V = v;
        }

        public double H;
        public double S;
        public double V;
    }
}

using System;
using System.Drawing;

namespace PixelParser.Converters.Formats
{
    [Serializable]
    public struct HclColor
    {
        public HclColor(double h, double c, double l)
        {
            H = h;
            C = c;
            L = l;
        }

        public double H;
        public double C;
        public double L;

        public LabColor ToLab()
        {
            var h = H * (Math.PI / 180);
            var a = C * Math.Cos(h);
            var b = C * Math.Sin(h);
            return new LabColor(L, a, b);
        }

        public Color ToRgb()
        {
            var lab = ToLab();
            return lab.ToRgb();
        }

        public override string ToString()
        {
            return String.Format("[H = {0}, C = {1}, L = {2}]", H, C, L);
        }
    }
}

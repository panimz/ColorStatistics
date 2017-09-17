using System;
using System.Drawing;

namespace PixelParser.Models
{
    [Serializable]
    class ColorStats: IComparable<ColorStats>
    {
        public string Name { get; set; }
        public int Argb { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte Alpha { get; set; }
        public float Hue { get; set; }
        public float Brightness { get; set; }
        public float Saturation { get; set; }
        public float Factor { get; set; }

        public ColorStats(Color color, float factor = 0)
        {
            Name = color.ToString();
            Argb = color.ToArgb();
            R = color.R;
            G = color.G;
            B = color.B;
            Alpha = color.A;
            Hue = color.GetHue();
            Brightness = color.GetBrightness();
            Saturation = color.GetSaturation();
            Factor = factor;
        }

        public int CompareTo(ColorStats other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }
            if (ReferenceEquals(null, other))
            {
                return 1;
            }
            if (R.CompareTo(other.R) != 0 ||
                G.CompareTo(other.G) != 0 || 
                B.CompareTo(other.B) != 0)
            {
                return 1;
            }
            return Alpha.CompareTo(other.Alpha);
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}


using PixelParser.Converters.Formats;

namespace PixelParser.Palette
{
    public sealed class PaletteOptions
    {
        public int HueMin;
        public int HueMax;
        public int ChromaMin;
        public int ChromaMax;
        public int LightMin;
        public int LightMax;
        public int Quality;
        public int Samples;

        public static PaletteOptions GetDefault()
        {
            return new PaletteOptions()
            {
                HueMin = 0,
                HueMax = 360,
                ChromaMin = 0,
                ChromaMax = 100,
                LightMin = 0,
                LightMax = 100,
                Quality = 50,
                Samples = 800
            };
        }

        public bool ValidateColor(LabColor color)
        {
            var hcl = color.ToHcl();
            return (hcl.H >= this.HueMin) &&
                   (hcl.H <= this.HueMin) &&
                   (hcl.C >= this.ChromaMin) &&
                   (hcl.C <= this.ChromaMax) &&
                   (hcl.L >= this.LightMin) &&
                   (hcl.L <= this.LightMax);
        }
    }
}

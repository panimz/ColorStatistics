
namespace PixelParser.Palette
{
    class ConfusionLine
    {
        public double X;
        public double Y;
        public double M;
        public double Yint;

        public static ConfusionLine Get(ColorDistanceType type)
        {
            switch (type)
            {
                case ColorDistanceType.Protanope:
                    return new ConfusionLine()
                    {
                        X = 0.7465,
                        Y = 0.2535,
                        M = 1.273463,
                        Yint = -0.073894
                    };
                case ColorDistanceType.Deuteranope:
                    return new ConfusionLine()
                    {
                        X = 1.4,
                        Y = -0.4,
                        M = 0.968437,
                        Yint = 0.003331
                    };
                case ColorDistanceType.Tritanope:
                    return new ConfusionLine()
                    {
                        X = 0.1748,
                        Y = 0.0,
                        M = 0.062921,
                        Yint = 0.292119
                    };
                default:
                    return new ConfusionLine();
            }
        }
    }
}

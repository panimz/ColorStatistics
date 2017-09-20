using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelParser.Palette
{
    public static class LabConstants
    {
        // Corresponds roughly to RGB brighter/darker
        public static int Kn = 18;

        // D65 standard referent
        public static double Xn = 0.950470;
        public static double Yn = 1;
        public static double Zn = 1.088830;

        public static double t0 = 0.137931034; // 4 / 29
        public static double t1 = 0.206896552;  // 6 / 29
        public static double t2 = 0.12841855;   // 3 * t1 * t1
        public static double t3 = 0.008856452;  // t1 * t1 * t1
    }
}

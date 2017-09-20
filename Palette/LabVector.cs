using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelParser.Palette
{
    class LabVector
    {
        public LabVector()
        {
            dL = 0.0;
            dA = 0.0;
            dB = 0.0;
        }

        public LabVector(double dl, double da, double db)
        {
            dL = dl;
            dA = da;
            dB = db;
        }

        public double dL { get; set; }
        public double dA { get; set; }
        public double dB { get; set; }
        public double Magnitude
        {
            get { return Math.Sqrt(dL * dL + dB * dB + dA * dA); }
        }
    }
}

using System;

namespace ProcessingModule
{
    public class EGUConverter
    {

		public double ConvertToEGU(double scalingFactor, double deviation, ushort rawValue)
        {
            return scalingFactor * rawValue + deviation;
        }

		public ushort ConvertToRaw(double scalingFactor, double deviation, double eguValue)
        {
            if (scalingFactor == 0) return 0; //izbegava potencijalno deljenje sa nulom
            double raw = (eguValue - deviation) / scalingFactor;
            return (ushort)Math.Round(raw);
        }
    }
}

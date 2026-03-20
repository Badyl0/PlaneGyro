using System;
using System.Collections.Generic;
using System.Text;

namespace PlaneGyroListner.Models
{
    internal class GearExtractor
    {
        public static int FromPercent(int percent)
        {
            if (percent < 100) return 0;
            return 1;
        }
    }
}

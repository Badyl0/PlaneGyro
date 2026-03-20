using System;
using System.Collections.Generic;
using System.Text;

namespace PlaneGyroListner.Models
{
    internal class FlapsExtractor
    {
        public static int FromPercent(int percent)
        {
            if (percent <= 15) return 0;
            else if (percent <= 25) return 1;
            else if (percent <= 90) return 2;
            else return 3;
        }
    }
}

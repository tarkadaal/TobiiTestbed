using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TobiiPlayground.Extensions
{
    public static class LongExtensions
    {
        public static DateTime UnixTimeStampToDateTime(this long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            dtDateTime = dtDateTime.AddTicks((long)unixTimeStamp * 10).ToLocalTime();
            return dtDateTime;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextechAREvents.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static string GetTimeZoneStandardName(this DateTimeOffset dtOffset)
        {
            var timeZones = TimeZoneInfo.GetSystemTimeZones();
            var tz = timeZones.Where(x => x.GetUtcOffset(dtOffset.DateTime).Equals(dtOffset.Offset)).ToList();
            var tzInfo = tz.FirstOrDefault(x => x.DisplayName.Contains("US")) ?? tz.FirstOrDefault();
            string tzName = tzInfo.StandardName;
            tzName = tzName.Contains("UTC") ? "UTC" : tzName; //Linux returns UTC-H:MM time zone list
            return tzName;
        }
    }
}

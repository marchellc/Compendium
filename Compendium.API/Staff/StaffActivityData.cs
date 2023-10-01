using helpers.Time;

using System;

namespace Compendium.Staff
{
    public class StaffActivityData
    {
        public long Total { get; set; } = 0;
        public long TwoWeeks { get; set; } = 0;

        public DateTime TwoWeeksStart { get; set; } = TimeUtils.LocalTime;
    }
}
using helpers.Time;

using System;

namespace Compendium.Staff
{
    public class StaffActivityData
    {
        public string UserId { get; set; }

        public long Total { get; set; } = 0;
        public long TotalOverwatch { get; set; } = 0;

        public long TwoWeeks { get; set; } = 0;
        public long TwoWeeksOverwatch { get; set; } = 0;

        public DateTime TwoWeeksStart { get; set; } = TimeUtils.LocalTime;
    }
}
using helpers.Time;

using System;

namespace Compendium.Activity
{
    public class ActivitySession
    {
        private DateTime _endedAt = DateTime.MinValue;

        public DateTime StartedAt { get; set; }

        public DateTime EndedAt
        {
            get => _endedAt;
            set
            {
                _endedAt = value;
                HasEnded = true;
            }
        }

        public bool HasEnded { get; set; }

        public TimeSpan Duration
        {
            get
            {
                if (!HasEnded)
                    return TimeUtils.LocalTime - StartedAt;
                else
                    return EndedAt - StartedAt;
            }
        }

        public bool IsBetween(DateTime min)
        {
            if (StartedAt < min)
                return false;

            return true;
        }
    }
}

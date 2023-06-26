using System;

namespace Compendium.State
{
    public struct CustomTimeIntervalStateData
    {
        public DateTime Last;
        public float Interval;

        public CustomTimeIntervalStateData(float interval)
        {
            Last = DateTime.Now;
            Interval = interval;
        }

        public bool CanUpdate() => (DateTime.Now - Last).TotalMilliseconds >= Interval;
        public void OnUpdate(float nextInterval)
        {
            Last = DateTime.Now;
            Interval = nextInterval;
        }
    }
}
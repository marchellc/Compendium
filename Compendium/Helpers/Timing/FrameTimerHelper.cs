using System.Diagnostics;

using UnityEngine;

namespace Compendium.Helpers.Timing
{
    public class FrameTimerHelper
    {
        private readonly Stopwatch m_Watch = new Stopwatch();

        public double LastFrameDurationExact { get; private set; } = -1;
        public double LastFrameDurationTicksExact { get; private set; }
        public double AverageFrameDurationExact => (MaxFrameDurationExact + MinFrameDurationExact) / 2f; 
        public double AverageFrameDurationTicksExact => (MaxFrameDurationTicksExact + MinFrameDurationTicksExact) / 2f; 
        public double MaxFrameDurationExact { get; private set; }
        public double MaxFrameDurationTicksExact { get; private set; }
        public double MinFrameDurationExact { get; private set; }
        public double MinFrameDurationTicksExact { get; private set; }
        public double TicksPerSecondExact { get; private set; }
        public double MaxTicksPerSecondExact { get; private set; }
        public double MinTicksPerSecondExact { get; private set; }
        public double AverageTicksPerSecondExact => (MaxTicksPerSecondExact + MinTicksPerSecondExact) / 2f;

        public int LastFrameDuration => Mathf.CeilToInt((float)LastFrameDurationExact);
        public int LastFrameDurationTicks => Mathf.CeilToInt((float)LastFrameDurationTicksExact);
        public int AverageFrameDuration => Mathf.CeilToInt((float)AverageFrameDurationExact);
        public int AverageFrameDurationTicks => Mathf.CeilToInt((float)AverageFrameDurationTicksExact);
        public int MaxFrameDuration => Mathf.CeilToInt((float)MaxFrameDurationExact);
        public int MaxFrameDurationTicks => Mathf.CeilToInt((float)MaxFrameDurationTicksExact);
        public int MinFrameDuration => Mathf.CeilToInt((float)MinFrameDurationExact);
        public int MinFrameDurationTicks => Mathf.CeilToInt((float)MinFrameDurationTicksExact);
        public int TicksPerSecond => Mathf.CeilToInt((float)TicksPerSecondExact);
        public int MaxTicksPerSecond => Mathf.CeilToInt((float)MaxTicksPerSecondExact);
        public int MinTicksPerSecond => Mathf.CeilToInt((float)MinTicksPerSecondExact);
        public int AverageTicksPerSecond => Mathf.CeilToInt((float)AverageTicksPerSecondExact);

        public int ExecutedFrames => Time.frameCount;

        public void UpdateStats()
        {
            m_Watch.Stop();

            LastFrameDurationExact = m_Watch.ElapsedMilliseconds;
            LastFrameDurationTicksExact = m_Watch.ElapsedTicks;

            TicksPerSecondExact = 1f / Time.deltaTime;

            if (LastFrameDurationExact > MaxFrameDurationExact) MaxFrameDurationExact = LastFrameDurationExact;
            if (LastFrameDurationTicksExact > MaxFrameDurationTicksExact) MaxFrameDurationTicksExact = LastFrameDurationTicksExact;
            if (LastFrameDurationExact < MinFrameDurationExact) MinFrameDurationExact = LastFrameDurationExact;
            if (LastFrameDurationTicksExact < MinFrameDurationTicksExact) MinFrameDurationTicksExact = LastFrameDurationTicksExact;

            if (TicksPerSecondExact > MaxTicksPerSecondExact) MaxTicksPerSecondExact = TicksPerSecondExact;
            if (TicksPerSecondExact < MinTicksPerSecondExact) MinTicksPerSecondExact = TicksPerSecondExact;

            m_Watch.Restart();
        }
    }
}
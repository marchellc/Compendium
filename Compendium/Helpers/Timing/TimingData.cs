using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEngine;

namespace Compendium.Helpers.Timing
{
    public struct TimingData
    {
        private static readonly HashSet<long> m_Average = new HashSet<long>();

        public double AverageExact;
        public double MinimumExact;
        public double MaximumExact;

        public int Total;
        public int Average;
        public int Minimum;
        public int Maximum;

        public void Record(Stopwatch stopwatch)
        {
            Total++;

            m_Average.Add(stopwatch.ElapsedMilliseconds);

            if (m_Average.Count >= 10)
            {
                AverageExact = m_Average.Average();
                Average = Mathf.CeilToInt((float)AverageExact);

                var minimum = m_Average.Min();
                if (minimum < MinimumExact)
                {
                    MinimumExact = minimum;
                    Minimum = Mathf.CeilToInt((float)MinimumExact);
                }

                var maximum = m_Average.Max();
                if (maximum > MaximumExact)
                {
                    MaximumExact = maximum;
                    Maximum = Mathf.CeilToInt((float)MaximumExact);
                }

                m_Average.Clear();
            }
        }

        public void Reset()
        {
            Total = 0;
            Average = 0;
            Minimum = 0;
            Maximum = 0;
            AverageExact = 0;
            MinimumExact = 0;
            MaximumExact = 0;
        }

        public static TimingData New() => new TimingData
        {
            Total = 0,
            Average = 0,
            Maximum = 0,
            Minimum = 0
        };
    }
}
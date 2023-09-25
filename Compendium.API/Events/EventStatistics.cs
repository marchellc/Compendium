using System.Collections.Generic;
using System.Linq;

namespace Compendium.Events
{
    public class EventStatistics
    {
        private List<double> _average = new List<double>();

        public double LongestTime { get; set; } = -1;
        public double ShortestTime { get; set; } = -1;
        public double AverageTime { get; set; } = -1;
        public double LastTime { get; set; } = -1;
        public double TicksWhenLongest { get; set; }

        public int Executions { get; set; } = 0;

        public void Reset()
        {
            _average.Clear();

            LongestTime = -1;
            ShortestTime = -1;
            AverageTime = -1;
            LastTime = -1;
            TicksWhenLongest = 0;
            Executions = 0;
        }

        public void Record(double time)
        {
            Executions++;
            LastTime = time;

            if (LongestTime is -1 || time > LongestTime)
            {
                LongestTime = time;
                TicksWhenLongest = World.TicksPerSecond;
            }

            if (ShortestTime is -1 || time < ShortestTime)
                ShortestTime = time;

            if (AverageTime is -1)
                AverageTime = time;

            _average.Add(time);

            if (_average.Count >= 10)
            {
                AverageTime = _average.Average();
                _average.Clear();
            }
        }

        public override string ToString()
            => 
            $"Longest: {LongestTime} ms\n" +
            $"Shortest: {ShortestTime} ms\n" +
            $"Last: {LastTime} ms\n" +
            $"Average: {AverageTime} ms\n" +
            $"Ticks When Highest: {TicksWhenLongest} TPS\n" +
            $"Total Executions: {Executions}";
    }
}
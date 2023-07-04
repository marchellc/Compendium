using System;
using System.Linq;

namespace Compendium.Webhooks
{
    public static class TimeUtils
    {
        public static string SecondsToCompoundTime(long seconds)
        {
            if (seconds <= 0) 
                return "0 sec";

            var span = TimeSpan.FromSeconds(seconds);
            var parts = new int[] { span.Days / 365, span.Days % 365 / 31,  span.Days % 365 % 31, span.Hours, span.Minutes, span.Seconds };
            var units = new string[] { " year", " month", " day", " hour", " minute", " second" };

            return string.Join(", ",
                from index in Enumerable.Range(0, units.Length)
                where parts[index] > 0
                select parts[index] + (parts[index] == 1 ? units[index] : units[index] + "s"));
        }

        public static string TicksToCompoundTime(long ticks)
        {
            return SecondsToCompoundTime(ticks / TimeSpan.TicksPerSecond);
        }
    }
}
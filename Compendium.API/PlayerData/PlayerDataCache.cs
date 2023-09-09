using helpers.Time;
using helpers.Values;

using System;
using System.Collections.Generic;

namespace Compendium.PlayerData
{
    public class PlayerDataCache
    {
        public DateTime LastChangeTime { get; set; } = DateTime.MinValue;

        public string LastValue { get; set; } = default;

        public Dictionary<DateTime, string> AllValues { get; set; } = new Dictionary<DateTime, string>();

        public bool Compare(string newValue)
        {
            if (newValue is null)
                return false;

            if (LastValue is null || LastValue != newValue)
            {
                LastValue = newValue;

                LastChangeTime = TimeUtils.LocalTime;
                AllValues[LastChangeTime] = LastValue;

                return true;
            }

            return false;
        }
    }
}
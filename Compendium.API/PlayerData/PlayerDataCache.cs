using helpers.Time;
using helpers.Values;

using System;
using System.Collections.Generic;

namespace Compendium.PlayerData
{
    public class PlayerDataCache<TValue> 
    {
        public DateTime LastChangeTime { get; set; } = DateTime.MinValue;
        public Optional<TValue> LastValue { get; set; } = Optional<TValue>.Null;

        public Dictionary<DateTime, TValue> AllValues { get; set; } = new Dictionary<DateTime, TValue>();

        public bool Compare(Optional<TValue> newValue)
        {
            if (!newValue.HasValue)
            {
                if (LastValue.HasValue)
                {
                    LastValue = Optional<TValue>.Null;
                    LastChangeTime = TimeUtils.LocalTime;

                    AllValues[LastChangeTime] = default;

                    return false;
                }

                return true;
            }

            if (!LastValue.HasValue)
            {
                LastValue.SetValue(newValue.Value);
                LastChangeTime = TimeUtils.LocalTime;

                AllValues[LastChangeTime] = LastValue.Value;

                return false;
            }

            if (!newValue.Value.Equals(LastValue.Value))
            {
                LastValue.SetValue(newValue.Value);
                LastChangeTime = TimeUtils.LocalTime;

                AllValues[LastChangeTime] = LastValue.Value;

                return false;
            }

            return true;
        }
    }
}
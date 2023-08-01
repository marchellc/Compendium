using helpers.Time;

using System;

namespace Compendium.PlayerData
{
    public class PlayerDataRecord
    {
        public string Id { get; set; }

        public PlayerDataCache<string> IdTracking { get; set; } = new PlayerDataCache<string>();
        public PlayerDataCache<string> IpTracking { get; set; } = new PlayerDataCache<string>();
        public PlayerDataCache<string> NameTracking { get; set; } = new PlayerDataCache<string>();

        public DateTime LastActivity { get; set; } = DateTime.MinValue;
        public DateTime CreationTime { get; set; } = TimeUtils.LocalTime;
    }
}

using helpers.Time;

using System;

namespace Compendium.PlayerData
{
    public class PlayerDataRecord
    {
        public string Id { get; set; } = "";
        public string Ip { get; set; } = "";
        public string UserId { get; set; } = "";

        public PlayerDataCache NameTracking { get; set; } = new PlayerDataCache();

        public DateTime LastActivity { get; set; } = DateTime.MinValue;
        public DateTime CreationTime { get; set; } = TimeUtils.LocalTime;
    }
}

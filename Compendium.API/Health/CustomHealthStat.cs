using Compendium.Events;

using PlayerStatsSystem;

using PluginAPI.Events;

namespace Compendium.Health
{
    public class CustomHealthStat : HealthStat
    {
        public override float MaxValue => CustomMaxValue == default ? base.MaxValue : CustomMaxValue;

        public float CustomMaxValue { get; set; }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            ev.Player.ReferenceHub.playerStats._dictionarizedTypes[typeof(HealthStat)] = (ev.Player.ReferenceHub.playerStats.StatModules[0] = new CustomHealthStat { Hub = ev.Player.ReferenceHub });
        }
    }
}
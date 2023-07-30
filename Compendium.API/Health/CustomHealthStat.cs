using PlayerStatsSystem;

namespace Compendium.Health
{
    public class CustomHealthStat : HealthStat
    {
        public override float MaxValue => CustomMaxValue == default ? base.MaxValue : CustomMaxValue;

        public float CustomMaxValue { get; set; }
    }
}
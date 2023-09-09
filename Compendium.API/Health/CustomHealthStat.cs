using Compendium.Events;

using helpers.Enums;

using PlayerStatsSystem;

using PluginAPI.Events;

namespace Compendium.Health
{
    public class CustomHealthStat : HealthStat
    {
        private float? _customMax = null;
        private CustomHealthFlags? _customFlags = null;

        public CustomHealthStat(ReferenceHub owner)
        {
            owner.playerStats._dictionarizedTypes[typeof(HealthStat)] = this;
            owner.playerStats.StatModules[0] = this;

            base.Init(owner);
        }

        public override float MaxValue
        {
            get
            {
                if (_customMax.HasValue)
                    return _customMax.Value;
                else
                    return base.MaxValue;
            }
        }

        public float MaxHealth
        {
            get => _customMax.HasValue ? _customMax.Value : base.MaxValue;
            set => _customMax = value;
        }

        public bool ShouldReset
        {
            get => !_customFlags.HasValue || !_customFlags.Value.HasFlagFast(CustomHealthFlags.NoReset);
            set
            {
                if (value)
                    _customFlags = CustomHealthFlags.NoReset;
                else
                    _customFlags = null;
            }
        }

        public override void ClassChanged()
        {
            if (ShouldReset)
            {
                _customFlags = null;
                _customMax = null;
            }

            base.ClassChanged();

            ServerHeal(MaxValue);
        }

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            _ = new CustomHealthStat(ev.Player.ReferenceHub);                     
        }
    }
}
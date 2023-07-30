using Compendium.Features;
using Compendium.Events;

using Compendium.ServerGuard.AccountShield;
using Compendium.ServerGuard.VpnShield;

using PluginAPI.Events;

namespace Compendium.ServerGuard
{
    public class ServerGuardFeature : ConfigFeatureBase
    {
        public override string Name => "Server Guard";

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev, ValueContainer isAllowed, ValueContainer shouldContinue)
        {
            VpnShieldHandler.Check(ev.Player.ReferenceHub, isKicked =>
            {
                if (isKicked)
                    return;

                AccountShieldHandler.Check(ev.Player.ReferenceHub);
            });
        }
    }
}
using PlayerRoles;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Helpers.Round
{
    public static class RoundHelper
    {
        public static bool IsStarted
        {
            get
            {
                try
                {
                    return RoundSummary.RoundInProgress();
                }
                catch { return false; }
            }
        }

        public static bool TryGenerateEndPreventingPlayerList(out List<ReferenceHub> hubs)
        {
            if (!IsStarted)
            {
                hubs = null;
                return false;
            }

            hubs = ReferenceHub.AllHubs.Where(hub => hub.Mode is ClientInstanceMode.ReadyClient && hub.IsAlive()).ToList();

            if (!hubs.Any())
                return false;

            if (hubs.Any(hub => hub.IsSCP()))
            {

            }

            return true;
        }
    }
}
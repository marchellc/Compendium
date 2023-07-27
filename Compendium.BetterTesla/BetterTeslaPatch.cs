using helpers;
using helpers.Patching;

using PlayerRoles;

using UnityEngine;

namespace Compendium.BetterTesla
{
    public static class BetterTeslaPatch
    {
        [Patch(typeof(TeslaGateController), nameof(TeslaGateController.FixedUpdate))]
        public static bool Prefix(TeslaGateController __instance)
        {
            var removed = __instance.TeslaGates.RemoveAll(tesla => tesla is null || tesla.gameObject is null || tesla.gameObject.transform is null);

            if (removed > 0)
                Log.Warn($"Removed {removed} invalid Tesla Gate instances!");

            if (BetterTeslaLogic.RoundDisabled)
                return false;

            __instance.TeslaGates.ForEach(tesla =>
            {
                if (tesla.isActiveAndEnabled)
                {
                    if (BetterTeslaLogic.AllowTeslaDamage)
                    {
                        var damage = BetterTeslaLogic.GetStatus(tesla);

                        if (damage.IsDisabled())
                        {
                            if (!tesla.isIdling)
                            {
                                tesla.ServerSideIdle(true);
                            }

                            return;
                        }
                    }

                    if (tesla.InactiveTime > 0f)
                    {
                        tesla.NetworkInactiveTime = Mathf.Max(0f, tesla.InactiveTime - Time.fixedDeltaTime);
                    }
                    else
                    {
                        var inIdleRange = false;
                        var inRange = false;

                        foreach (var hub in ReferenceHub.AllHubs)
                        {
                            if (hub.Mode != ClientInstanceMode.ReadyClient)
                                continue;

                            if (!hub.IsAlive())
                                continue;

                            if (BetterTeslaLogic.RoundDisabledRoles.Contains(hub.GetRoleId()))
                                continue;

                            if (BetterTeslaLogic.IgnoredRoles.Contains(hub.GetRoleId()))
                                continue;

                            if (!inIdleRange)
                                inIdleRange = tesla.IsInIdleRange(hub);

                            if (!inRange && tesla.PlayerInRange(hub) && !tesla.InProgress)
                                inRange = true;
                        }

                        if (inRange)
                            tesla.ServerSideCode();

                        if (inIdleRange != tesla.isIdling)
                            tesla.ServerSideIdle(inIdleRange);
                    }
                }
            });

            return false;
        }
    }
}
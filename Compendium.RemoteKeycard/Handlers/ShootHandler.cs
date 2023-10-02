using CustomPlayerEffects;

using helpers.Patching;

using InventorySystem.Items.Firearms.Modules;

using PlayerStatsSystem;

using System;

using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers
{
    public static class ShootHandler
    {
        public static event Func<ReferenceHub, Ray, RaycastHit, GameObject, bool> OnHit;

        [Patch(typeof(SingleBulletHitreg), nameof(SingleBulletHitreg.ServerProcessRaycastHit))]
        private static bool RaycastHitReplacement(SingleBulletHitreg __instance, Ray ray, RaycastHit hit)
        {
            if (RoundSwitches.IsShotDisabled)
                return true;

            if (OnHit != null && !OnHit(__instance.Hub, ray, hit, hit.collider.gameObject))
                return false;

            if (hit.collider.TryGetComponent<IDestructible>(out var destructible) 
                && __instance.CheckInaccurateFriendlyFire(destructible))
            {
                var damage = __instance.Firearm.BaseStats.DamageAtDistance(__instance.Firearm, hit.distance);

                if (destructible.Damage(damage, new FirearmDamageHandler(__instance.Firearm, damage), hit.point))
                {
                    if (!ReferenceHub.TryGetHubNetID(destructible.NetworkId, out var targetHub) 
                        || !targetHub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
                        Hitmarker.SendHitmarker(__instance.Conn, 1f);

                    __instance.ShowHitIndicator(destructible.NetworkId, damage, ray.origin);
                    __instance.PlaceBloodDecal(ray, hit, destructible);
                }
            }
            else
                __instance.PlaceBulletholeDecal(ray, hit);

            return false;
        }
    }
}

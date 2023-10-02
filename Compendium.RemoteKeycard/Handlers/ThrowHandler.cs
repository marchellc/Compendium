using helpers.Patching;

using System;

using UnityEngine;

using InventorySystem.Items.Pickups;

using PlayerRoles.PlayableScps.Scp939.Ripples;

namespace Compendium.RemoteKeycard.Handlers
{
    public static class ThrowHandler
    {
        public static event Func<ReferenceHub, GameObject, bool> OnThrown;

        [Patch(typeof(CollisionDetectionPickup), nameof(CollisionDetectionPickup.ProcessCollision), PatchType.Prefix)]
        private static bool ThrowPatch(CollisionDetectionPickup __instance, Collision collision)
        {
            if (OnThrown != null && !OnThrown(__instance.PreviousOwner.Hub, collision.gameObject))
                return false;

            PickupRippleTrigger.OnCollided(__instance, collision);

            var sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
            var num = __instance.Info.WeightKg * sqrMagnitude / 2f;

            if (num > 15f)
            {
                var damage = num * 0.4f;

                if (collision.collider.TryGetComponent<BreakableWindow>(out var breakableWindow))
                    breakableWindow.Damage(damage, null, Vector3.zero);
            }

            return false;
        }
    }
}
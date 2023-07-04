using Compendium.Extensions;
using Compendium.Grab.Targets;

using InventorySystem.Items.Pickups;

using UnityEngine;

namespace Compendium.Grab
{
    public static class GrabObserver
    {
        public static bool TryObserve(ReferenceHub hub, out IGrabTarget target)
        {
            var ray = new Ray(hub.PlayerCameraReference.position, hub.PlayerCameraReference.forward);
            var mask = LayerMask.GetMask("Pickup", "Player", "Ragdoll", "Grenade", "Door", "Locker", "SCP018");

            if (!Physics.Raycast(ray, out var hit, 30f, mask, QueryTriggerInteraction.Ignore))
            {
                target = null;
                return false;
            }

            var transform = hit.transform;

            if (transform.parent != null)
                transform = transform.parent;

            if (transform.gameObject.TryGet<ItemPickupBase>(out var pickup))
            {
                target = new PickupTarget(pickup, hub);
                return true;
            }

            if (transform.gameObject.TryGet<ReferenceHub>(out var targetHub))
            {
                target = new HubTarget(targetHub, hub);
                return true;
            }

            target = null;
            return false;
        }
    }
}
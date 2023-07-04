using BetterCommands;

using Compendium.Extensions;
using Compendium.Grab.Targets;

using InventorySystem.Items.Pickups;

using PluginAPI.Core;

using UnityEngine;

namespace Compendium.Grab
{
    public static class GrabCommands
    {
        [Command("grab", CommandType.RemoteAdmin)]
        public static string Grab(Player sender,  [LookingAt(30f, "Pickup", "Player", "Ragdoll", "Grenade", "Door", "Locker", "SCP018")] GameObject target)
        {
            if (target.TryGet<ItemPickupBase>(out var pickup))
            {
                GrabHandler.Grab(sender.ReferenceHub, new PickupTarget(pickup, sender.ReferenceHub));
                return $"Grabbed pickup: {pickup.Info.ItemId}";
            }

            if (target.TryGet<ReferenceHub>(out var targetHub))
            {
                GrabHandler.Grab(sender.ReferenceHub, new HubTarget(targetHub, sender.ReferenceHub));
                return $"Grabbed player: {targetHub.LoggedNameFromRefHub()}";
            }

            return $"Unsupported grab target: {target.name}";
        }

        [Command("ungrab", CommandType.RemoteAdmin)]
        public static string Ungrab(Player sender)
        {
            GrabHandler.Ungrab(sender.ReferenceHub);
            return $"Ungrabbed.";
        }
    }
}
using Compendium.RemoteKeycard.Handlers;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using System.Linq;

namespace Compendium.RemoteKeycard
{
    public static class AccessUtils
    {
        public static bool CanAccessWarhead(ReferenceHub player)
            => player.inventory.UserInventory.Items.Any(x => x.Value != null && x.Value is KeycardItem keycard 
            && keycard.Permissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
            && !RoundSwitches.IsRemote;

        public static bool CanAccessChamber(LockerChamber chamber, ReferenceHub player)
            => chamber.RequiredPermissions is KeycardPermissions.None || player.inventory.UserInventory.Items.Any(x => x.Value != null 
            && x.Value is KeycardItem keycard && keycard.Permissions.HasFlagFast(chamber.RequiredPermissions))
            && !RoundSwitches.IsRemote;

        public static bool CanAccessGenerator(Scp079Generator generator, ReferenceHub player)
            => player.inventory.UserInventory.Items.Any(x => x.Value != null && x.Value is KeycardItem keycard 
            && keycard.Permissions.HasFlagFast(generator._requiredPermission))
            && !RoundSwitches.IsRemote;

        public static bool CanAccessDoor(DoorVariant door, ReferenceHub player)
            => player.inventory.UserInventory.Items.Any(x => x.Value != null && x.Value is KeycardItem keycard
            && door.RequiredPermissions.CheckPermissions(keycard, player))
            && !RoundSwitches.IsRemote;
    }
}

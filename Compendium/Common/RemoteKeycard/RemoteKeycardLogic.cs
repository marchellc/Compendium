using Compendium.Features;
using Compendium.Helpers.Patching;

using Interactables.Interobjects.DoorUtils;

using MapGeneration.Distributors;

namespace Compendium.Common.RemoteKeycard
{
    public class RemoteKeycardLogic : FeatureBase
    {
        public override string Name { get; } = "Remote Keycard";

        public static PatchData Patch => new PatchData()
            .WithType(PatchType.Prefix);

        public static readonly PatchData DoorPatch = Patch
            .WithReplacement(typeof(RemoteKeycardPatch), nameof(RemoteKeycardPatch.DoorPatch))
            .WithTarget(typeof(DoorVariant), nameof(DoorVariant.ServerInteract))
            .WithName("Remote Keycard Door Patch");

        public static readonly PatchData LockerPatch = Patch
            .WithReplacement(typeof(RemoteKeycardPatch), nameof(RemoteKeycardPatch.LockerPatch))
            .WithTarget(typeof(Locker), nameof(Locker.ServerInteract))
            .WithName("Remote Keycard Locker Patch");

        public static readonly PatchData PanelPatch = Patch
            .WithReplacement(typeof(RemoteKeycardPatch), nameof(RemoteKeycardPatch.PanelPatch))
            .WithTarget(typeof(PlayerInteract), nameof(PlayerInteract.UserCode_CmdSwitchAWButton))
            .WithName("Remote Keycard Panel Patch");

        public override void OnLoad() => PatchManager.ApplyPatches(DoorPatch, LockerPatch, PanelPatch);
        public override void OnUnload() => PatchManager.UnapplyPatches(DoorPatch, LockerPatch, PanelPatch);

        public bool HasPermission(DoorPermissions doorPermissions, ReferenceHub player)
        {
            foreach (var item in player.inventory.UserInventory.Items.Values)
            {
                if (doorPermissions.CheckPermissions(item, player))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

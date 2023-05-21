using Compendium.Features;

using Interactables.Interobjects.DoorUtils;

using PlayerRoles;

using PluginAPI.Enums;
using PluginAPI.Events;

namespace Compendium.Common.RemoteKeycard
{
    public static class RemoteKeycardPatch
    {
        public static bool DoorPatch(ReferenceHub ply, byte colliderId, DoorVariant __instance)
        {
            if (__instance.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
            {
                var mode = DoorLockUtils.GetMode((DoorLockReason)__instance.NetworkActiveLocks);
                if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) 
                    && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock 
                    || (__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
                {
                    if (!EventManager.ExecuteEvent(ServerEventType.PlayerInteractDoor, ply, __instance, false)) return false;
                    __instance.LockBypassDenied(ply, colliderId);
                    DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
                    return false;
                }
            }

            if (!__instance.AllowInteracting(ply, colliderId)) return false;
            var hasPerms = ply.GetRoleId() is RoleTypeId.Scp079 || __instance.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);
            if (!hasPerms 
                && FeatureManager.TryGetFeature<RemoteKeycardLogic>(out var remoteKeycard) 
                && remoteKeycard.IsRunning
                && remoteKeycard.HasPermission(__instance.RequiredPermissions, ply))
            {
                hasPerms = true;
            }

            if (!EventManager.ExecuteEvent(ServerEventType.PlayerInteractDoor, ply, __instance, hasPerms)) return false;
            if (hasPerms)
            {
                __instance.NetworkTargetState = !__instance.TargetState;
                __instance._triggerPlayer = ply;
                return false;
            }

            __instance.PermissionsDenied(ply, colliderId);
            DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
            return false;
        }

        public static bool LockerPatch()
        {

        }

        public static bool PanelPatch()
        {

        }
    }
}

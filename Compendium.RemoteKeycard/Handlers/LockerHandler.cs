using helpers.Configuration;
using helpers.Patching;

using Interactables.Interobjects.DoorUtils;

using MapGeneration.Distributors;

using PluginAPI.Events;

using System;

namespace Compendium.RemoteKeycard.Handlers
{
    [ConfigCategory(Name = "Locker")]
    public static class LockerHandler
    {
        [Config(Name = "Enabled", Description = "Whether or not to enable remote interactions for lockers.")]
        public static bool IsEnabled { get; set; } = true;

        [Patch(typeof(Locker), nameof(Locker.ServerInteract))]
        private static bool LockerInteractionReplacement(Locker __instance, ReferenceHub ply, byte colliderId)
        {
            if (RoundSwitches.IsLockerDisabled)
                return false;

            try
            {
                if (colliderId >= __instance.Chambers.Length || !__instance.Chambers[colliderId].CanInteract)
                    return false;

                var canOpen = false;

                if (ply.serverRoles.BypassMode)
                    canOpen = true;

                if (__instance.Chambers[colliderId].RequiredPermissions is KeycardPermissions.None)
                    canOpen = true;

                if (!canOpen)
                    canOpen = __instance.CheckPerms(__instance.Chambers[colliderId].RequiredPermissions, ply);

                if (!canOpen)
                    canOpen = AccessUtils.CanAccessChamber(__instance.Chambers[colliderId], ply);

                if (!EventManager.ExecuteEvent(new PlayerInteractLockerEvent(ply, __instance, __instance.Chambers[colliderId], canOpen)))
                    return false;

                if (!canOpen)
                {
                    __instance.RpcPlayDenied(colliderId);
                    return false;
                }

                __instance.Chambers[colliderId].SetDoor(!__instance.Chambers[colliderId].IsOpen, __instance._grantedBeep);
                __instance.RefreshOpenedSyncvar();

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception in the locker patch!\n{ex}");
                return true;
            }
        }
    }
}

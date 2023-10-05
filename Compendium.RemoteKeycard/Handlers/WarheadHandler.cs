using Compendium.Enums;
using Compendium.Attributes;

using helpers.Configuration;
using helpers.Patching;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using Respawning;

using System;

using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers
{
    [ConfigCategory(Name = "Warhead")]
    public static class WarheadHandler
    {
        public static AlphaWarheadOutsitePanel Panel;
        public static GameObject Script;

        [Config(Name = "Toggleable", Description = "Whether or not to allow players with sufficient perms to toggle the alpha warhead keycard button.")]
        public static bool IsToggleable { get; set; } = true;

        [Config(Name = "Permission", Description = "The permission required to access the alpha warhead button.")]
        public static KeycardPermissions Permission { get; set; } = KeycardPermissions.AlphaWarhead;

        [Patch(typeof(PlayerInteract), nameof(PlayerInteract.UserCode_CmdSwitchAWButton), PatchType.Prefix)]
        private static bool WarheadButtonReplacement(PlayerInteract __instance)
        {
            if (RoundSwitches.IsWarheadDisabled)
                return false;

            try
            {
                if (!__instance.CanInteract)
                    return false;

                if (Script is null || Panel is null)
                    return false;

                if (!__instance.ChckDis(Script.transform.position))
                    return false;

                if (IsToggleable && Panel.NetworkkeycardEntered)
                {
                    Panel.NetworkkeycardEntered = false;
                    __instance.OnInteract();
                    return false;
                }

                if (!__instance._sr.BypassMode)
                {
                    var canUse = false;

                    if (__instance._inv._curInstance != null)
                    {
                        if (__instance._inv._curInstance is KeycardItem keycard)
                        {
                            if (keycard.Permissions.HasFlagFast(Permission))
                                canUse = true;
                        }
                    }

                    if (AccessUtils.CanAccessWarhead(__instance._hub))
                        canUse = true;

                    if (!canUse)
                        return false;
                }

                __instance.OnInteract();

                Panel.NetworkkeycardEntered = IsToggleable ? !Panel.NetworkkeycardEntered : true;

                if (__instance._hub.TryGetAssignedSpawnableTeam(out var team))
                    RespawnTokensManager.GrantTokens(team, 1f);

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception in the warhead patch!\n{ex}");
                return true;
            }
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnRoundWaiting()
        {
            Panel = null;
            Script = null;
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStart()
        {
            Script = GameObject.Find("OutsitePanelScript");
            Panel = Script.GetComponentInParent<AlphaWarheadOutsitePanel>();
        }
    }
}
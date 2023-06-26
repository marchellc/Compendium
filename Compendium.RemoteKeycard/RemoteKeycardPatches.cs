using Footprinting;

using helpers.Patching;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using PlayerRoles;

using PluginAPI.Events;

using Respawning;

using UnityEngine;

using System;

namespace Compendium.RemoteKeycard
{
    public static class RemoteKeycardPatches
    {
        public static readonly PatchInfo DoorInteractionPatch = new PatchInfo(
            new PatchTarget(typeof(DoorVariant), nameof(DoorVariant.ServerInteract)),
            new PatchTarget(typeof(RemoteKeycardPatches), nameof(RemoteKeycardPatches.DoorInteractionReplacement)), PatchType.Prefix, "Door Interaction Patch [RK]");

        public static readonly PatchInfo GeneratorInteractionPatch = new PatchInfo(
            new PatchTarget(typeof(Scp079Generator), nameof(Scp079Generator.ServerInteract)),
            new PatchTarget(typeof(RemoteKeycardPatches), nameof(RemoteKeycardPatches.GeneratorInteractionReplacement)), PatchType.Prefix, "Generation Interaction Patch [RK]");

        public static readonly PatchInfo LockerInteractionPatch = new PatchInfo(
            new PatchTarget(typeof(Locker), nameof(Locker.ServerInteract)),
            new PatchTarget(typeof(RemoteKeycardPatches), nameof(RemoteKeycardPatches.LockerInteractionReplacement)), PatchType.Prefix, "Locker Interaction Patch [RK]");

        public static readonly PatchInfo WarheadButtonPatch = new PatchInfo(
            new PatchTarget(typeof(PlayerInteract), nameof(PlayerInteract.UserCode_CmdSwitchAWButton)),
            new PatchTarget(typeof(RemoteKeycardPatches), nameof(RemoteKeycardPatches.WarheadButtonReplacement)), PatchType.Prefix, "Warhead Interaction Patch [RK]");

        private static bool WarheadButtonReplacement(PlayerInteract __instance)
        {
            try
            {
                if (!__instance.CanInteract)
                    return false;

                GameObject gameObject = GameObject.Find("OutsitePanelScript");

                if (!__instance.ChckDis(gameObject.transform.position))
                    return false;

                AlphaWarheadOutsitePanel componentInParent = gameObject.GetComponentInParent<AlphaWarheadOutsitePanel>();

                if (componentInParent == null)
                {
                    return false;
                }

                if (!__instance._sr.BypassMode)
                {
                    var canUse = false;

                    if (__instance._inv._curInstance != null)
                    {
                        if (__instance._inv._curInstance is KeycardItem keycard)
                        {
                            if (keycard.Permissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
                            {
                                canUse = true;
                            }
                        }
                    }

                    if (RemoteKeycardLogic.CanBypass(__instance._hub))
                    {
                        canUse = true;
                    }

                    if (!canUse)
                    {
                        return false;
                    }
                }

                __instance.OnInteract();

                componentInParent.NetworkkeycardEntered = !componentInParent.keycardEntered;

                if (__instance._hub.TryGetAssignedSpawnableTeam(out var team))
                {
                    RespawnTokensManager.GrantTokens(team, 1f);
                }

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception in the warhead patch!\n{ex}");
                return true;
            }
        }

        private static bool LockerInteractionReplacement(Locker __instance, ReferenceHub ply, byte colliderId)
        {
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
                    canOpen = RemoteKeycardLogic.CanBypass(ply, __instance.Chambers[colliderId]);

                if (!EventManager.ExecuteEvent(new PlayerInteractLockerEvent(ply, __instance, __instance.Chambers[colliderId], canOpen)))
                {
                    return false;
                }

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

        private static bool GeneratorInteractionReplacement(Scp079Generator __instance, ReferenceHub ply, byte colliderId)
        {
            try
            {
                if (__instance._cooldownStopwatch.IsRunning && __instance._cooldownStopwatch.Elapsed.TotalSeconds < __instance._targetCooldown)
                    return false;

                if (colliderId != 0 && !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open))
                    return false;

                __instance._cooldownStopwatch.Stop();

                if (!EventManager.ExecuteEvent(new PlayerInteractGeneratorEvent(ply, __instance, (Scp079Generator.GeneratorColliderId)colliderId)))
                {
                    __instance._cooldownStopwatch.Restart();
                    return false;
                }

                switch (colliderId)
                {
                    case 0:
                        if (__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Unlocked))
                        {
                            if (__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open))
                            {
                                if (!EventManager.ExecuteEvent(new PlayerCloseGeneratorEvent(ply, __instance)))
                                {
                                    break;
                                }
                            }
                            else if (!EventManager.ExecuteEvent(new PlayerOpenGeneratorEvent(ply, __instance)))
                            {
                                break;
                            }

                            __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open));
                            __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        }
                        else
                        {
                            bool flag = false;

                            if (!ply.serverRoles.BypassMode)
                            {
                                if (ply.inventory.CurInstance != null)
                                {
                                    KeycardItem keycardItem = ply.inventory.CurInstance as KeycardItem;

                                    if (keycardItem != null)
                                    {
                                        flag = keycardItem.Permissions.HasFlagFast(__instance._requiredPermission);
                                    }
                                }

                                if (!flag)
                                {
                                    if (RemoteKeycardLogic.CanBypass(ply, __instance))
                                    {
                                        flag = true;
                                    }
                                }
                            }
                            else
                            {
                                flag = true;
                            }

                            if (!flag)
                            {
                                __instance._targetCooldown = __instance._unlockCooldownTime;
                                __instance.RpcDenied();
                            }
                            else if (EventManager.ExecuteEvent(new PlayerUnlockGeneratorEvent(ply, __instance)))
                            {
                                __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Unlocked, true);
                                __instance.ServerGrantTicketsConditionally(new Footprint(ply), 0.5f);
                            }
                        }

                        break;
                    case 1:
                        if ((ply.IsHuman() || __instance.Activating) && !__instance.Engaged)
                        {
                            if (!__instance.Activating)
                            {
                                if (!EventManager.ExecuteEvent(new PlayerActivateGeneratorEvent(ply, __instance)))
                                {
                                    break;
                                }
                            }
                            else if (!EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                            {
                                break;
                            }

                            __instance.Activating = !__instance.Activating;

                            if (__instance.Activating)
                            {
                                __instance._leverStopwatch.Restart();
                                __instance._lastActivator = new Footprint(ply);
                            }
                            else
                            {
                                __instance._lastActivator = default(Footprint);
                            }

                            __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        }
                        break;
                    case 2:
                        if (__instance.Activating && !__instance.Engaged && EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                        {
                            __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, false);
                            __instance._targetCooldown = __instance._unlockCooldownTime;
                            __instance._lastActivator = default(Footprint);
                        }

                        break;
                    default:
                        __instance._targetCooldown = 1f;
                        break;
                }

                __instance._cooldownStopwatch.Restart();
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception in the generator patch!\n{ex}");
                return true;
            }
        }

        private static bool DoorInteractionReplacement(DoorVariant __instance, ReferenceHub ply, byte colliderId)
        {
            try
            {
                if (__instance.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
                {
                    var mode = DoorLockUtils.GetMode((DoorLockReason)__instance.ActiveLocks);

                    if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock || (__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!__instance.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
                    {
                        if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, false)))
                            return false;

                        __instance.LockBypassDenied(ply, colliderId);
                        return false;
                    }
                }

                if (!__instance.AllowInteracting(ply, colliderId))
                    return false;

                var flag = ply.GetRoleId() == RoleTypeId.Scp079 || __instance.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);

                if (!flag)
                {
                    if (RemoteKeycardLogic.CanBypass(ply, __instance))
                        flag = true;
                }

                if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, flag)))
                    return false;

                if (flag)
                {
                    __instance.NetworkTargetState = !__instance.TargetState;
                    __instance._triggerPlayer = ply;

                    return false;
                }

                __instance.PermissionsDenied(ply, colliderId);

                DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Caught an exception in the door patch!\n{ex}");
                return true;
            }
        }
    }
}
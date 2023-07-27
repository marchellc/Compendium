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

using InventorySystem.Items.Firearms.Modules;

using CustomPlayerEffects;

using PlayerStatsSystem;

using Compendium.Extensions;

using Interactables.Interobjects;

using System.Collections.Generic;

using Mirror;

namespace Compendium.RemoteKeycard
{
    public static class RemoteKeycardPatches
    {
        [Patch(typeof(SingleBulletHitreg), nameof(SingleBulletHitreg.ServerProcessRaycastHit))]
        private static bool RaycastHitReplacement(SingleBulletHitreg __instance, Ray ray, RaycastHit hit)
        {
            if (hit.collider.TryGetComponent<IDestructible>(out var destructible) && __instance.CheckInaccurateFriendlyFire(destructible))
            {
                var damage = __instance.Firearm.BaseStats.DamageAtDistance(__instance.Firearm, hit.distance);

                if (destructible.Damage(damage, new FirearmDamageHandler(__instance.Firearm, damage), hit.point))
                {
                    if (!ReferenceHub.TryGetHubNetID(destructible.NetworkId, out var targetHub) || !targetHub.playerEffectsController.GetEffect<Invisible>().IsEnabled)
                    {
                        Hitmarker.SendHitmarker(__instance.Conn, 1f);
                    }

                    __instance.ShowHitIndicator(destructible.NetworkId, damage, ray.origin);
                    __instance.PlaceBloodDecal(ray, hit, destructible);
                }    
            }
            else
            {
                __instance.PlaceBulletholeDecal(ray, hit);
            }

            if (RemoteKeycardLogic.AllowShots)
            {
                Physics.Raycast(__instance.Hub.PlayerCameraReference.position, __instance.Hub.PlayerCameraReference.forward, out RaycastHit raycastHit, 70f, ~(1 << 13 | 1 << 16));

                if (raycastHit.collider is null)
                    return false;

                var gameObject = raycastHit.transform.gameObject;

                if (gameObject.TryGet<RegularDoorButton>(out var button))
                {
                    var door = button.GetComponentInParent<DoorVariant>();
                    if (door != null)
                    {
                        door.ServerInteract(__instance.Hub, 0);
                    }
                }
                else if (gameObject.TryGet<ElevatorPanel>(out var panel))
                {
                    if (panel.AssignedChamber != null 
                        && panel.AssignedChamber.IsReady
                        && ElevatorDoor.AllElevatorDoors.TryGetValue(panel.AssignedChamber.AssignedGroup, out List<ElevatorDoor> list))
                    {
                        int nextLevel = panel.AssignedChamber.CurrentLevel + 1;

                        if (nextLevel >= list.Count)
                            nextLevel = 0;

                        panel.AssignedChamber.TrySetDestination(nextLevel);

                        NetworkServer.SendToReady(new ElevatorManager.ElevatorSyncMsg(panel.AssignedChamber.AssignedGroup, panel.AssignedChamber.CurrentLevel));
                        ElevatorManager.SyncedDestinations[panel.AssignedChamber.AssignedGroup] = panel.AssignedChamber.CurrentLevel;
                    }
                }
            }

            return false;
        }

        [Patch(typeof(PlayerInteract), nameof(PlayerInteract.UserCode_CmdSwitchAWButton))]
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

        [Patch(typeof(Locker), nameof(Locker.ServerInteract))]
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

        [Patch(typeof(Scp079Generator), nameof(Scp079Generator.ServerInteract))]
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
    }
}
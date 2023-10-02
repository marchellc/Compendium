using Compendium.Features;

using Footprinting;

using helpers.Configuration;
using helpers.Patching;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using PlayerRoles;

using PluginAPI.Events;

using System;

namespace Compendium.RemoteKeycard.Handlers
{
    [ConfigCategory(Name = "Generator")]
    public static class GeneratorHandler
    {
        [Config(Name = "Enabled", Description = "Whether or not to enable remote interactions for generators.")]
        public static bool IsEnabled { get; set; } = true;

        [Patch(typeof(Scp079Generator), nameof(Scp079Generator.ServerInteract), PatchType.Prefix)]
        private static bool GeneratorInteractionReplacement(Scp079Generator __instance, ReferenceHub ply, byte colliderId)
        {
            if (RoundSwitches.IsGeneratorDisabled)
                return false;

            if (!IsEnabled)
                return true;

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
                                    break;
                            }
                            else if (!EventManager.ExecuteEvent(new PlayerOpenGeneratorEvent(ply, __instance)))
                                break;

                            __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Open, !__instance.HasFlag(__instance._flags, Scp079Generator.GeneratorFlags.Open));
                            __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        }
                        else
                        {
                            var flag = false;

                            if (!ply.serverRoles.BypassMode)
                            {
                                if (ply.inventory.CurInstance != null)
                                {
                                    var keycardItem = ply.inventory.CurInstance as KeycardItem;

                                    if (keycardItem != null)
                                        flag = keycardItem.Permissions.HasFlagFast(__instance._requiredPermission);   
                                }

                                if (!flag && AccessUtils.CanAccessGenerator(__instance, ply))
                                    flag = true;
                            }
                            else
                                flag = true;

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
                                    break;
                            }
                            else if (!EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                                break;

                            __instance.Activating = !__instance.Activating;

                            if (__instance.Activating)
                            {
                                __instance._leverStopwatch.Restart();
                                __instance._lastActivator = new Footprint(ply);
                            }
                            else
                                __instance._lastActivator = default;

                            __instance._targetCooldown = __instance._doorToggleCooldownTime;
                        }

                        break;

                    case 2:
                        if (__instance.Activating && !__instance.Engaged && EventManager.ExecuteEvent(new PlayerDeactivatedGeneratorEvent(ply, __instance)))
                        {
                            __instance.ServerSetFlag(Scp079Generator.GeneratorFlags.Activating, false);
                            __instance._targetCooldown = __instance._unlockCooldownTime;
                            __instance._lastActivator = default;
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
                FLog.Error($"Caught an exception in the generator patch!\n{ex}");
                return true;
            }
        }
    }
}

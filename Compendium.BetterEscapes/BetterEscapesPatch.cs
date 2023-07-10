using Compendium.Extensions;
using Compendium.Features;
using helpers;
using helpers.Patching;

using InventorySystem.Disarming;
using InventorySystem.Items;

using PlayerRoles;

using PluginAPI.Events;

using Respawning;
using System.Linq;
using UnityEngine;

namespace Compendium.BetterEscapes
{
    public static class BetterEscapesPatch
    {
        public const float MaxMagnitude = 156.6f;

        public static readonly PatchInfo patch = new PatchInfo(
            new PatchTarget(typeof(Escape), nameof(Escape.ServerHandlePlayer)),
            new PatchTarget(typeof(BetterEscapesPatch), nameof(BetterEscapesPatch.Prefix)), PatchType.Prefix, "Escape Patch");

        public static readonly PatchInfo disarmPatch = new PatchInfo(
            new PatchTarget(typeof(DisarmedPlayers), nameof(DisarmedPlayers.CanDisarm)),
            new PatchTarget(typeof(BetterEscapesPatch), nameof(BetterEscapesPatch.DisarmingPrefix)), PatchType.Prefix, "Disarming Patch");

        public static bool DisarmingPrefix(ReferenceHub disarmerHub, ReferenceHub targetHub, ref bool __result)
        {
            if (disarmerHub.GetFaction() == targetHub.GetFaction())
            {
                if (!BetterEscapesLogic.AllowTeamDisarming || (disarmerHub.IsSCP(true) && targetHub.IsSCP(true) && !BetterEscapesLogic.AllowScpDisarming))
                {
                    __result = false;
                    return false;
                }
            }

            if (!disarmerHub.IsHuman() || !targetHub.IsHuman())
            {
                if (!BetterEscapesLogic.AllowScpDisarming)
                {
                    __result = false;
                    return false;
                }
            }

            if (targetHub.interCoordinator.AnyBlocker(BlockedInteraction.BeDisarmed))
            {
                __result = false;
                return false;
            }

            if (BetterEscapesLogic.RequireItemDisarming && !disarmerHub.IsSCP(true))
            {
                var curItem = disarmerHub.inventory.CurInstance;

                if (curItem != null)
                {
                    if (BetterEscapesLogic.OverrideItemDisarming)
                    {
                        if (!BetterEscapesLogic.DisarmingItems.Contains(curItem.ItemTypeId))
                        {
                            __result = false;
                            return false;
                        }
                        else
                        {
                            __result = true;
                            return false;
                        }
                    }

                    if (BetterEscapesLogic.DisarmingItems.Contains(curItem.ItemTypeId))
                    {
                        __result = true;
                        return false;
                    }

                    if (curItem is IDisarmingItem disarmingItem)
                    {
                        __result = disarmingItem.AllowDisarming;
                        return false;
                    }
                }
                else
                {
                    __result = false;
                    return false;
                }
            }

            __result = false;
            return false;
        }

        private static bool Prefix(ReferenceHub hub)
        {
            RoleTypeId newRole = RoleTypeId.None;

            Escape.EscapeScenarioType escapeScenarioType = Escape.ServerGetScenario(hub);

            switch (escapeScenarioType)
            {
                case Escape.EscapeScenarioType.None:
                    {
                        if (hub.IsWithinDistance(Escape.WorldPos, MaxMagnitude))
                        {
                            var disarmerId = DisarmedPlayers.Entries.FirstOrDefault(entry => entry.DisarmedPlayer == hub.netId).Disarmer;

                            RoleTypeId? disarmerRole = null;

                            if (ReferenceHub.TryGetHubNetID(disarmerId, out var disarmer) && disarmer != null)
                                disarmerRole = disarmer.GetRoleId();

                            if (BetterEscapesLogic.TryOverride(hub.GetRoleId(), disarmerRole, out var ovRole))
                            {
                                if (ovRole.HasValue)
                                {
                                    newRole = ovRole.Value;
                                    FLog.Info($"Overriden escape role ({hub.GetRoleId()} -> {newRole})");
                                }

                                break;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                case Escape.EscapeScenarioType.ClassD:
                case Escape.EscapeScenarioType.CuffedScientist:
                    newRole = RoleTypeId.ChaosConscript;
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.ChaosInsurgency, 4f);
                    break;
                case Escape.EscapeScenarioType.CuffedClassD:
                    newRole = RoleTypeId.NtfPrivate;
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.NineTailedFox, 3f);
                    break;
                case Escape.EscapeScenarioType.Scientist:
                    newRole = RoleTypeId.NtfSpecialist;
                    RespawnTokensManager.GrantTokens(SpawnableTeamType.NineTailedFox, 3f);
                    break;
            }

            if (newRole is RoleTypeId.None)
                return false;

            if (!EventManager.ExecuteEvent(new PlayerEscapeEvent(hub, newRole)))
                return false;

            FLog.Info($"{hub.nicknameSync.Network_myNickSync} escaped as {newRole}!");

            hub.connectionToClient.Send(new Escape.EscapeMessage
            {
                ScenarioId = (byte)escapeScenarioType,
                EscapeTime = (ushort)Mathf.CeilToInt(hub.roleManager.CurrentRole.ActiveTime)
            });

            Reflection.TryInvokeEvent(typeof(Event), "OnServerPlayerEscape", null, hub);

            hub.roleManager.ServerSetRole(newRole, RoleChangeReason.Escaped, RoleSpawnFlags.All);
            return false;
        }
    }
}
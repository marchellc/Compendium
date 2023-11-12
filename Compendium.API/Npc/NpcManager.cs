using BetterCommands;

using Compendium.Events;
using Compendium.Extensions;
using Compendium.Npc.Targeting;
using Compendium.Prefabs;
using Compendium.Enums;
using Compendium.Attributes;

using helpers;
using helpers.Patching;

using NorthwoodLib.Pools;

using HarmonyLib;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;

using PluginAPI.Events;

using CentralAuth;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace Compendium.Npc
{
    public static class NpcManager
    {
        private static readonly HashSet<NpcPlayer> m_Spawned = new HashSet<NpcPlayer>();
        private static readonly HashSet<NpcPlayer> m_Despawned = new HashSet<NpcPlayer>();
        private static readonly HashSet<NpcPlayer> m_All = new HashSet<NpcPlayer>();

        public static IReadOnlyCollection<NpcPlayer> Spawned => m_Spawned;
        public static IReadOnlyCollection<NpcPlayer> Despawned => m_Despawned;
        public static IReadOnlyCollection<NpcPlayer> All => m_All;

        public static HashSet<ReferenceHub> NpcHubs = new HashSet<ReferenceHub>();

        public static ReferenceHub NewHub
        {
            get
            {
                if (!PrefabHelper.TryInstantiatePrefab(PrefabName.Player, out var hubObj))
                    return null;

                var hub = hubObj.GetComponent<ReferenceHub>();

                NpcHubs.Add(hub);
                return hub;
            }
        }

        internal static void OnNpcCreated(NpcPlayer npc)
        {
            m_All.Add(npc);
        }

        internal static void OnNpcDespawned(NpcPlayer npc)
        {
            m_Spawned.Remove(npc);
            m_Despawned.Add(npc);
        }

        internal static void OnNpcDestroyed(NpcPlayer npc)
        {
            m_Despawned.Remove(npc);
            m_Spawned.Remove(npc);
            m_All.Remove(npc);
        }

        internal static void OnNpcSpawned(NpcPlayer npc)
        {
            m_Despawned.Remove(npc);
            m_Spawned.Add(npc);
        }

        [RoundStateChanged(RoundState.Ending)]
        internal static void OnRoundEnd()
        {
            m_All.ForEach(npc =>
            {
                npc.Destroy();
            });

            m_All.Clear();
            m_Spawned.Clear();
            m_Despawned.Clear();
        }

        [BetterCommands.Command("spawnnpc", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("snpc")]
        [Description("Spawns an NPC of the specified role at your position.")]
        private static string SpawnCommand(ReferenceHub sender, RoleTypeId role)
        {
            var npc = new NpcPlayer();

            npc.Spawn();

            Calls.Delay(2f, () =>
            {
                npc.Hub.RoleId(role, RoleSpawnFlags.All);

                Calls.Delay(0.5f, () =>
                {
                    npc.Teleport(sender.Position());
                });
            });

            return $"Spawned an NPC with ID: '{npc.CustomId}'";
        }

        [Command("tptonpc", CommandType.RemoteAdmin)]
        [CommandAliases("ttnpc")]
        [Description("Teleports you to the targeted NPC.")]
        private static string TeleportToNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            sender.Position(npc.Position);
            return "Teleported you to the targeted NPC.";
        }

        [Command("tpnpc", CommandType.RemoteAdmin)]
        [CommandAliases("tnpc")]
        [Description("Teleports the targeted NPC to your position.")]
        private static string TeleportNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Teleport(sender.Position());
            return "Teleported the targeted NPC to you.";
        }

        [Command("npcnick", CommandType.RemoteAdmin)]
        [CommandAliases("nnick")]
        [Description("Changes the nick of the targeted NPC")]
        private static string NpcNickCommand(ReferenceHub sender, string npcId, string nick)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Nick = nick;
            return $"Set the NPC's nick to '{nick}'";
        }

        [Command("despawnnpc", CommandType.RemoteAdmin)]
        [CommandAliases("desnpc")]
        [Description("Despawns the targeted NPC.")]
        private static string DespawnNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Despawn();
            return "Despawned the targeted NPC.";
        }

        [Command("destroynpc", CommandType.RemoteAdmin)]
        [CommandAliases("destnpc")]
        [Description("Destroys the targeted NPC.")]
        private static string DestroyNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Destroy();
            return "Destroyed the targeted NPC.";
        }

        [Command("npcrole", CommandType.RemoteAdmin)]
        [CommandAliases("npcr")]
        [Description("Sets the role of the targeted NPC.")]
        private static string NpcRoleCommand(ReferenceHub sender, string npcId, RoleTypeId role)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.RoleId = role;
            return $"Set role of the targeted NPC to '{role}'";
        }

        [Command("npcfollow", CommandType.RemoteAdmin)]
        [CommandAliases("npcf")]
        [Description("Sets the target of the targeted NPC.")]
        private static string NpcFollowCommand(ReferenceHub sender, string npcId, ReferenceHub target)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Target = new PlayerTarget(target);
            return $"The targeted NPC is now following '{target.Nick()}'";
        }

        [Command("npcstopfollow", CommandType.RemoteAdmin)]
        [CommandAliases("npcsfollow")]
        [Description("Stops the targeted NPC from following.")]
        private static string NpcStopFollowCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Target = null;
            return "The targeted NPC should no longer follow anyone.";
        }

        [Command("npcspeed", CommandType.RemoteAdmin)]
        [CommandAliases("npcsp")]
        [Description("Sets the speed of the targeted NPC.")]
        private static string NpcSpeedCommand(ReferenceHub sender, string npcId, float speed)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            if (speed <= 0f)
            {
                npc.ForcedSpeed = null;
                return "Removed forced speed from the targeted NPC.";
            }
            else
            {
                npc.ForcedSpeed = speed;
                return $"Forced speed of the targeted NPC to {speed}";
            }
        }

        [Command("npclist", CommandType.RemoteAdmin)]
        [CommandAliases("npcl")]
        [Description("Lists all existing NPCs.")]
        private static string NpcListCommand(ReferenceHub sender)
        {
            if (!All.Any())
                return "There aren't any known NPCs.";

            var sb = new StringBuilder();

            All.For((i, npc) =>
            {
                sb.AppendLine($"[{i + 1}] {npc.CustomId} {npc.RoleId} [{(Spawned.Contains(npc) ? "SPAWNED" : "DESPAWNED")}] (distance: {npc.Position.DistanceSquared(sender.Position())})");
            });

            return sb.ToString();
        }

        [Command("npcinvis", CommandType.RemoteAdmin)]
        [CommandAliases("npci")]
        [Description("Toggles the targeted NPC's invisibility.")]
        private static string NpcInvisCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            if (npc.Hub.IsInvisible())
            {
                npc.Hub.MakeVisible();
                return "The targeted NPC is now visible.";
            }
            else
            {
                npc.Hub.MakeInvisible();
                return "The targeted NPC is now invisible.";
            }
        }

        [Command("npcitem", CommandType.RemoteAdmin)]
        [CommandAliases("npcit")]
        [Description("Sets the currently held item by the targeted NPC.")]
        private static string NpcItemCommand(ReferenceHub sender, string npcId, ItemType item)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.HeldItem = item;
            return $"Set the currently held item to {item}";
        }

        [Event]
        private static void OnRoleChanged(PlayerChangeRoleEvent ev)
        {
            var hub = ev.Player.ReferenceHub;

            if (hub.IsNpc())
            {
                hub.SetTargetRole(RoleTypeId.Spectator, 0, Hub.Hubs.Where(h => h.RoleId() == RoleTypeId.Spectator).ToArray());
                hub.SetTargetRole(hub.RoleId(), 0, Hub.Hubs.Where(h => h.RoleId() != RoleTypeId.Spectator).ToArray());
            }
            else
            {
                if (ev.NewRole is RoleTypeId.Spectator)
                    NpcHubs.ForEach(h => h.SetTargetRole(RoleTypeId.Spectator, 0, hub));
                else
                    NpcHubs.ForEach(h => h.SetTargetRole(h.RoleId(), 0, hub));
            }
        }

        [Patch(typeof(PlayerAuthenticationManager), nameof(PlayerAuthenticationManager.InstanceMode), PatchType.Transpiler, PatchMethodType.PropertySetter)]
        private static IEnumerable<CodeInstruction> InstanceModeSetterPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
            var skip = generator.DefineLabel();

            newInstructions[0].labels.Add(skip);
            newInstructions.InsertRange(0, new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NpcManager), nameof(NpcManager.NpcHubs))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CharacterClassManager), nameof(CharacterClassManager._hub))),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<ReferenceHub>), nameof(HashSet<ReferenceHub>.Contains))),
                new CodeInstruction(OpCodes.Brfalse_S, skip),
                new CodeInstruction(OpCodes.Ldc_I4_2),
                new CodeInstruction(OpCodes.Starg_S, 1),
            });

            foreach (CodeInstruction instruction in newInstructions)
                yield return instruction;

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }

        [Patch(typeof(FpcMouseLook), nameof(FpcMouseLook.UpdateRotation), PatchType.Transpiler)]
        private static IEnumerable<CodeInstruction> RotationPatch(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);
            var skip = generator.DefineLabel();

            newInstructions[newInstructions.Count - 1].labels.Add(skip);
            newInstructions.InsertRange(0, new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FpcMouseLook), nameof(FpcMouseLook._hub))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NpcHelper), nameof(NpcHelper.IsNpc))),
                new CodeInstruction(OpCodes.Brtrue_S, skip)
            });

            foreach (CodeInstruction instruction in newInstructions)
                yield return instruction;

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }

        [Patch(typeof(ReferenceHub), nameof(ReferenceHub.OnDestroy), PatchType.Prefix)]
        private static bool OnDestroyPatch(ReferenceHub __instance)
        {
            if (!__instance.isLocalPlayer && !__instance.IsNpc() && !NpcHubs.Contains(__instance))
                EventManager.ExecuteEvent(new PlayerLeftEvent(__instance));

            ReferenceHub.AllHubs.Remove(__instance);
            ReferenceHub.HubsByGameObjects.Remove(__instance.gameObject);
            ReferenceHub.HubByPlayerIds.Remove(__instance.PlayerId);

            __instance._playerId.Destroy();

            if (ReferenceHub._hostHub == __instance)
            {
                ReferenceHub._hostHub = null;
                ReferenceHub._hostHubSet = false;
            }

            if (ReferenceHub._localHub == __instance)
            {
                ReferenceHub._localHub = null;
                ReferenceHub._localHubSet = false;
            }

            ReferenceHub.OnPlayerRemoved?.Invoke(__instance);
            return false;
        }

        [Patch(typeof(ReferenceHub), nameof(ReferenceHub.Awake), PatchType.Prefix)]
        private static bool AwakePatch(ReferenceHub __instance)
        {
            if (!NpcHubs.Contains(__instance))
            {
                ReferenceHub.AllHubs.Add(__instance);
                ReferenceHub.HubsByGameObjects[__instance.gameObject] = __instance;
            }

            __instance.Network_playerId = new RecyclablePlayerId(true);
            __instance.FriendlyFireHandler = new FriendlyFireHandler(__instance);

            return false;
        }
    }
}
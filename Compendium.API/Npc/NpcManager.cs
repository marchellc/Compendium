using BetterCommands;

using Compendium.Extensions;
using Compendium.Invisibility;
using Compendium.Npc.Targeting;
using Compendium.Prefabs;
using Compendium.Round;

using HarmonyLib;

using helpers.Extensions;
using helpers.Patching;

using NorthwoodLib.Pools;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PluginAPI.Events;
using RemoteAdmin;
using RemoteAdmin.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using VoiceChat;

namespace Compendium.Npc
{
    public static class NpcManager
    {
        private static readonly HashSet<INpc> m_Spawned = new HashSet<INpc>();
        private static readonly HashSet<INpc> m_Despawned = new HashSet<INpc>();
        private static readonly HashSet<INpc> m_All = new HashSet<INpc>();

        public static IReadOnlyCollection<INpc> Spawned => m_Spawned;
        public static IReadOnlyCollection<INpc> Despawned => m_Despawned;
        public static IReadOnlyCollection<INpc> All => m_All;

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

        internal static void OnNpcCreated(INpc npc)
        {
            m_All.Add(npc);
        }

        internal static void OnNpcDespawned(INpc npc)
        {
            m_Spawned.Remove(npc);
            m_Despawned.Add(npc);
        }

        internal static void OnNpcDestroyed(INpc npc)
        {
            m_Despawned.Remove(npc);
            m_Spawned.Remove(npc);
            m_All.Remove(npc);
        }

        internal static void OnNpcSpawned(INpc npc)
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
            var npc = new NpcBase();

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

        [BetterCommands.Command("tptonpc", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("ttnpc")]
        [Description("Teleports you to the targeted NPC.")]
        private static string TeleportToNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            sender.Position(npc.Position);
            return "Teleported you to the targeted NPC.";
        }

        [BetterCommands.Command("tpnpc", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("tnpc")]
        [Description("Teleports the targeted NPC to your position.")]
        private static string TeleportNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Teleport(sender.Position());
            return "Teleported the targeted NPC to you.";
        }

        [BetterCommands.Command("npcnick", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("nnick")]
        [Description("Changes the nick of the targeted NPC")]
        private static string NpcNickCommand(ReferenceHub sender, string npcId, string nick)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Nick = nick;
            return $"Set the NPC's nick to '{nick}'";
        }

        [BetterCommands.Command("despawnnpc", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("desnpc")]
        [Description("Despawns the targeted NPC.")]
        private static string DespawnNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Despawn();
            return "Despawned the targeted NPC.";
        }

        [BetterCommands.Command("destroynpc", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("destnpc")]
        [Description("Destroys the targeted NPC.")]
        private static string DestroyNpcCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Destroy();
            return "Destroyed the targeted NPC.";
        }

        [BetterCommands.Command("npcrole", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("npcr")]
        [Description("Sets the role of the targeted NPC.")]
        private static string NpcRoleCommand(ReferenceHub sender, string npcId, RoleTypeId role)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.RoleId = role;
            return $"Set role of the targeted NPC to '{role}'";
        }

        [BetterCommands.Command("npcfollow", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("npcf")]
        [Description("Sets the target of the targeted NPC.")]
        private static string NpcFollowCommand(ReferenceHub sender, string npcId, ReferenceHub target)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Target = new PlayerTarget(target);
            return $"The targeted NPC is now following '{target.Nick()}'";
        }

        [BetterCommands.Command("npcstopfollow", BetterCommands.CommandType.RemoteAdmin)]
        [CommandAliases("npcsfollow")]
        [Description("Stops the targeted NPC from following.")]
        private static string NpcStopFollowCommand(ReferenceHub sender, string npcId)
        {
            if (!All.TryGetFirst(n => n.CustomId == npcId, out var npc))
                return "Failed to find an NPC with that ID.";

            npc.Target = null;
            return "The targeted NPC should no longer follow anyone.";
        }

        [BetterCommands.Command("npcspeed", BetterCommands.CommandType.RemoteAdmin)]
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

        [BetterCommands.Command("npclist", BetterCommands.CommandType.RemoteAdmin)]
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

        [BetterCommands.Command("npcinvis", BetterCommands.CommandType.RemoteAdmin)]
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

        [Patch(typeof(CharacterClassManager), nameof(CharacterClassManager.InstanceMode), PatchType.Transpiler, PatchMethodType.PropertySetter)]
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

        [Patch(typeof(RaPlayerList), nameof(RaPlayerList.ReceiveData), PatchType.Prefix, typeof(CommandSender), typeof(string))]
        private static bool RaPlayerListPatch(RaPlayerList __instance, CommandSender sender, string data)
        {
            var array = data.Split(' ');

            if (array.Length != 3)
                return false;

            if (!int.TryParse(array[0], out var num) || !int.TryParse(array[1], out var num2))
                return false;

            if (!Enum.IsDefined(typeof(RaPlayerList.PlayerSorting), num2))
                return false;

            var flag = num == 1;
            var flag2 = array[2].Equals("1");
            var sortingType = (RaPlayerList.PlayerSorting)num2;
            var viewHiddenBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
            var viewHiddenGlobalBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
            var playerCommandSender = sender as PlayerCommandSender;

            if (playerCommandSender != null && playerCommandSender.ServerRoles.Staff)
            {
                viewHiddenBadges = true;
                viewHiddenGlobalBadges = true;
            }

            var stringBuilder = StringBuilderPool.Shared.Rent("\n");

            foreach (var referenceHub in (flag2 ? __instance.SortPlayersDescending(sortingType) : __instance.SortPlayers(sortingType)))
            {
                if (referenceHub.Mode != ClientInstanceMode.DedicatedServer 
                    && referenceHub.Mode != ClientInstanceMode.Unverified
                    && referenceHub.characterClassManager._targetInstanceMode != ClientInstanceMode.DedicatedServer
                    && !referenceHub.IsNpc())
                {
                    var isInOverwatch = referenceHub.serverRoles.IsInOverwatch;
                    var flag3 = VoiceChatMutes.IsMuted(referenceHub, false);

                    stringBuilder.Append(__instance.GetPrefix(referenceHub, viewHiddenBadges, viewHiddenGlobalBadges));

                    if (isInOverwatch)
                    {
                        stringBuilder.Append("<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc></color><color=white>]</color></link> ");
                    }
                    if (flag3)
                    {
                        stringBuilder.Append("<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ");
                    }

                    stringBuilder.Append("<color={RA_ClassColor}>(").Append(referenceHub.PlayerId).Append(") ");
                    stringBuilder.Append(referenceHub.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
                    stringBuilder.AppendLine();
                }
            }

            sender.RaReply(string.Format("${0} {1}", __instance.DataId, StringBuilderPool.Shared.ToStringReturn(stringBuilder)), true, !flag, string.Empty);
            return false;
        }

        [Patch(typeof(ReferenceHub), nameof(ReferenceHub.OnDestroy), PatchType.Prefix)]
        private static bool OnDestroyPatch(ReferenceHub __instance)
        {
            if (!__instance.isLocalPlayer && !__instance.IsNpc())
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
    }
}
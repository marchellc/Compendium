using BetterCommands;

using Compendium.Extensions;
using Compendium.Invisibility;
using Compendium.Npc.Targeting;
using Compendium.Prefabs;
using Compendium.Round;

using helpers.Extensions;
using helpers.Patching;

using PlayerRoles;

using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static ReferenceHub NewHub
        {
            get
            {
                if (!PrefabHelper.TryInstantiatePrefab(PrefabName.Player, out var hubObj))
                    return null;

                return hubObj.GetComponent<ReferenceHub>();
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

        [Patch(typeof(CharacterClassManager), nameof(CharacterClassManager.InstanceMode), PatchType.Prefix, PatchMethodType.PropertyGetter)]
        private static bool NpcInstanceModeGetPatch(CharacterClassManager __instance, ref ClientInstanceMode __result)
        {
            if (All.Any(n => n.Hub != null && n.Hub == __instance.Hub))
            {
                __result = ClientInstanceMode.Host;
                return false;
            }

            return true;
        }

        [Patch(typeof(CharacterClassManager), nameof(CharacterClassManager.InstanceMode), PatchType.Prefix, PatchMethodType.PropertySetter)]
        private static bool NpcInstanceModeSetPatch(CharacterClassManager __instance, ref ClientInstanceMode value)
        {
            if (All.Any(n => n.Hub != null && n.Hub == __instance.Hub))
                value = ClientInstanceMode.Host;

            return true;
        }
    }
}
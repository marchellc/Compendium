using BetterCommands;

using Compendium.Attributes;
using Compendium.Features;
using Compendium.PlayerData;

using helpers.Configuration;
using helpers.Random;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp3114;

using System.Collections.Generic;

namespace Compendium.Gameplay.Spawning
{
    public static class SpawnHandler
    {
        private static int roundsSinceReset = 0;
        private static List<string> scp3114Players = new List<string>();

        [Config(Name = "SCP-3114 Spawn Chance", Description = "The chance of SCP-3114 spawning.")]
        public static int Scp3114Chance { get; set; } = 30;

        [Config(Name = "SCP-3114 Player Chance", Description = "The chance for a player to be chosen as SCP-3114")]
        public static int PlayerChance { get; set; } = 5;

        public static bool IsForced;
        public static string ForcedUserId;

        public static ReferenceHub Chosen3114;

        [RoundStateChanged(Enums.RoundState.InProgress)]
        public static void Spawn3114()
        {
            roundsSinceReset++;

            if (roundsSinceReset >= 5)
            {
                roundsSinceReset = 0;
                scp3114Players.Clear();
            }

            Choose3114();

            if (Chosen3114 is null)
                return;

            Chosen3114.roleManager.ServerSetRole(RoleTypeId.Scp3114, RoleChangeReason.RoundStart, RoleSpawnFlags.All);

            Scp3114Spawner.SpawnRagdolls(Chosen3114.Nick());

            FLog.Info($"Spawned {Chosen3114.Nick()} as SCP-3114");
        }

        public static void Choose3114()
        {
            if ((IsForced || !string.IsNullOrWhiteSpace(ForcedUserId))
                || (Scp3114Chance > 0 && Hub.Count >= 10))
            {
                if (!string.IsNullOrWhiteSpace(ForcedUserId) && Hub.TryGetHub(ForcedUserId, out Chosen3114))
                {
                    FLog.Info($"Chosen {Chosen3114.Nick()} as SCP-3114; forced");
                    return;
                }
                else
                {
                    if (WeightedRandomGeneration.Default.GetBool(Scp3114Chance))
                    {
                        foreach (var hub in Hub.Hubs)
                        {
                            if (Chosen3114 != null)
                                continue;

                            if (hub.RoleId() is RoleTypeId.Overwatch)
                                continue;

                            if (scp3114Players.Contains(hub.UserId()))
                                continue;

                            var chance = Scp3114Chance - PlayerChance;

                            if (chance <= 0 || !WeightedRandomGeneration.Default.GetBool(chance))
                                continue;

                            Chosen3114 = hub;
                            scp3114Players.Add(hub.UserId());
                            FLog.Info($"Chosen {Chosen3114.Nick()} as SCP-3114; chance");
                            break;
                        }
                    }
                }

                if (Chosen3114 is null)
                    FLog.Warn($"SCP-3114 is not spawning this round; no players meet conditions.");
            }
            else
                FLog.Warn($"SCP-3114 is not spawning this round; conditions not met.");
        }

        [Command("force3114spawn", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Forces SCP-3114 to spawn next round.")]
        public static string Force3114SpawnCommand(ReferenceHub sender)
        {
            IsForced = !IsForced;

            if (!IsForced)
                return "Disabled force spawning of SCP-3114.";
            else
            {
                if (RoundHelper.IsStarted)
                    return "SCP-3114 will spawn next round.";
                else
                    return "SCP-3114 will spawn this round.";
            }
        }

        [Command("force3114player", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Forces SCP-3114 to spawn next round as a specific player.")]
        public static string Force3114PlayerCommand(ReferenceHub sender, PlayerDataRecord target)
        {
            if (ForcedUserId != null && ForcedUserId == target.UserId)
            {
                ForcedUserId = null;
                return $"Disabled forced SCP-3114 spawning for '{target.NameTracking.LastValue}'";
            }
            else
            {
                ForcedUserId = target.UserId;
                return $"Enabled forced SCP-3114 spawning for '{target.NameTracking.LastValue}'";
            }
        }
    }
}
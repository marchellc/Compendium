using Compendium.Attributes;

using helpers.Configuration;

using PlayerRoles;

using PluginAPI.Core;

using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Spawning
{
    public static class SpawnRoleChooser
    {
        [Config(Name = "Default Role", Description = "The role to give if the chooser fails to choose a role OR the role queue runs out.")]
        public static RoleTypeId DefaultRole { get; set; } = RoleTypeId.ClassD;

        [Config(Name = "Lone 079", Description = "Whether or not to enable spawning of SCP-079 without any other SCPs.")]
        public static bool SpawnLone079 { get; set; } = true;

        [Config(Name = "NTF On Start Chance", Description = "The chance of NTF players spawning at the start of the round.")]
        public static int NtfOnStart { get; set; }

        [Config(Name = "NTF On Start Players", Description = "The minimum amount of players required for NTF to spawn.")]
        public static int NtfOnStartMinPlayers { get; set; } = Mathf.CeilToInt(Server.MaxPlayers / 4);

        [Config(Name = "CI On Start Chance", Description = "The chance of CI players spawning at the start of the round.")]
        public static int CiOnStart { get; set; }

        [Config(Name = "CI On Start Players", Description = "The minimum amount of players required for CI to spawn.")]
        public static int CiOnStartMinPlayers { get; set; } = Mathf.CeilToInt(Server.MaxPlayers / 2);

        [Config(Name = "Spawn Queue", Description = "Spawn queue.")]
        public static Team[] SpawnQueue { get; set; } = new Team[]
        {
            Team.ClassD,
            Team.SCPs,
            Team.Scientists,
            Team.FoundationForces
        };

        [Config(Name = "Disabled Roles", Description = "A list of roles that won't spawn.")]
        public static List<RoleTypeId> DisabledRoles { get; set; } = new List<RoleTypeId>();

        [Config(Name = "Roles", Description = "A list of roles that can be spawned.")]
        public static List<SpawnRoleInfo> Roles { get; set; } = new List<SpawnRoleInfo>()
        {

        };

        public static List<Team> TeamQueue;

        [RoundStateChanged(Enums.RoundState.WaitingForPlayers)]
        public static void Load()
        {
            var teamPos = 0;
            var plyCount = Server.MaxPlayers + ReservedSlot.Users.Count + 20;

            TeamQueue = new List<Team>(plyCount);

            for (int i = 0; i < plyCount; i++)
            {
                if (SpawnQueue.Length <= 0)
                    TeamQueue.Add(DefaultRole.GetTeam());
                else
                {
                    var team = SpawnQueue[teamPos];

                    if (team is Team.Dead)
                        TeamQueue.Add(DefaultRole.GetTeam());
                    else
                        TeamQueue.Add(team);

                    teamPos++;

                    if (teamPos >= SpawnQueue.Length)
                        teamPos = 0;
                }
            }
        }

        public static RoleTypeId Choose(ReferenceHub hub)
        {

            return DefaultRole;
        }
    }
}
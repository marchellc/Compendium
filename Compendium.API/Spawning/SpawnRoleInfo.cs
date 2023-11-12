using PlayerRoles;

using PluginAPI.Core;

namespace Compendium.Spawning
{
    public class SpawnRoleInfo
    {
        public RoleTypeId Role { get; set; } = RoleTypeId.None;

        public int Chance { get; set; } = 100;
        public int Players { get; set; } = Server.MaxPlayers / 2;
        public int Count { get; set; } = 1;
    }
}
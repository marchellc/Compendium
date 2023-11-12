using PlayerRoles;

using System.Collections.Generic;

namespace Compendium.Spawning
{
    public class SpawnChanceInfo
    {
        public string Id { get; set; }

        public Dictionary<RoleTypeId, int> Chance { get; set; } = new Dictionary<RoleTypeId, int>();
    }
}
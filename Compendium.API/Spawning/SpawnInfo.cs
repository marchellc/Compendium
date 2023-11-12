using MapGeneration;

using PlayerRoles;

using UnityEngine;

namespace Compendium.Spawning
{
    public class SpawnInfo
    {
        public readonly RoleTypeId Role;
        public readonly Vector3 Position;
        public readonly RoomIdentifier Room;

        public SpawnInfo(RoleTypeId role, Vector3 position, RoomIdentifier room)
        {
            Role = role;
            Position = position;
            Room = room;
        }
    }
}
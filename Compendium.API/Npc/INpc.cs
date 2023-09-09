using Compendium.Npc.Targeting;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using System;
using System.Collections.Generic;

using UnityEngine;

namespace Compendium.Npc
{
    public interface INpc
    {
        ReferenceHub Hub { get; }

        Scp079Camera Camera { get; set; }

        ITarget Target { get; set; }

        Vector3 Position { get; }
        Vector3 Rotation { get; }
        Vector3 Scale { get; set; }

        NpcMovementMode CurMovementMode { get; }
        NpcMovementMode? ForcedMode { get; }

        RoleTypeId RoleId { get; set; }
        PlayerRoleBase Role { get; set; }

        Dictionary<NpcMovementMode, float> Distancing { get; }
        Dictionary<NpcMovementMode, float> Speed { get; }

        bool IsSpawned { get; }
        bool Enable079Logic { get; }

        string Nick { get; set; }
        string UserId { get; set; }
        string CustomId { get; set; }

        int Id { get; set; }

        float CurrentSpeed { get; }
        float? ForcedSpeed { get; set; }

        void Teleport(Vector3 location);
        void Move(Vector3 destination);
        void Spawn(Action<INpc> modify = null);
        void Despawn();
        void Destroy();
    }
}
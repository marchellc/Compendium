using Compendium.Extensions;

using helpers;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;

using System;
using System.Linq;

using UnityEngine;

namespace Compendium.Npc
{
    public static class NpcHelper
    {
        public static NpcPlayer Spawn(Vector3 position, RoleTypeId role, ItemType heldItem)
        {
            var npc = new NpcPlayer();

            npc.Spawn();

            Calls.Delay(0.2f, () =>
            {
                npc.Teleport(position);
                npc.RoleId = role;
                npc.HeldItem = heldItem;
            });

            return npc;
        }

        public static bool IsNpc(this ReferenceHub hub)
            => TryGetNpc(hub, out _);

        public static NpcPlayer GetNpc(this ReferenceHub hub)
            => TryGetNpc(hub, out var npc) ? npc : null;

        public static bool TryGetNpc(this ReferenceHub hub, out NpcPlayer npc)
            => NpcManager.All.TryGetFirst(n => n.Hub != null && n.Hub == hub, out npc);

        public static Scp079Camera GetClosestCamera(Vector3 target)
        {
            var cameras = Scp079InteractableBase.AllInstances.Where<Scp079Camera>();
            var orderedCameras = cameras.OrderByDescending(cam => cam.DistanceSquared(target));

            return orderedCameras.FirstOrDefault();
        }

        public static PlayerMovementState TranslateMode(NpcMovementMode npcMovementMode)
        {
            switch (npcMovementMode)
            {
                case NpcMovementMode.Running:
                    return PlayerMovementState.Sprinting;

                case NpcMovementMode.Walking:
                    return PlayerMovementState.Walking;

                default:
                    throw new InvalidOperationException($"{npcMovementMode} cannot be translated.");
            }
        }
    }
}
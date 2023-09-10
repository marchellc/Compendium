using Compendium.Extensions;

using helpers.Extensions;

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
        public static bool IsNpc(this ReferenceHub hub)
            => TryGetNpc(hub, out _);

        public static INpc GetNpc(this ReferenceHub hub)
            => TryGetNpc(hub, out var npc) ? npc : null;

        public static bool TryGetNpc(this ReferenceHub hub, out INpc npc)
            => NpcManager.All.TryGetFirst(n => n.Hub != null && n.Hub == hub, out npc);

        public static Scp079Camera GetClosestCamera(Vector3 target)
        {
            var cameras = Scp079InteractableBase.AllInstances.Where<Scp079Camera>();
            var orderedCameras = cameras.OrderByDescending(cam => cam.DistanceSquared(target));

            return orderedCameras.FirstOrDefault();
        }

        public static void ForceMove(ReferenceHub hub, Vector3 direction)
        {
            if (hub is null)
                return;

            if (hub.roleManager is null)
                return;

            if (hub.roleManager.CurrentRole is null)
                return;

            if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                return;

            if (fpcRole is null)
                return;

            if (fpcRole.FpcModule is null)
                return;

            if (!fpcRole.FpcModule.CharControllerSet || fpcRole.FpcModule.CharController is null)
                return;

            fpcRole.FpcModule.CharController.Move(direction);
        }

        public static void ForceRotation(ReferenceHub hub, float rotationX, float rotationY)
        {
            if (hub is null)
                return;

            if (hub.roleManager is null)
                return;

            if (hub.roleManager.CurrentRole is null)
                return;

            if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                return;

            if (fpcRole is null)
                return;

            if (fpcRole.FpcModule is null)
                return;

            if (fpcRole.FpcModule.MouseLook is null)
                return;

            fpcRole.FpcModule.MouseLook.CurrentVertical = rotationY;
            fpcRole.FpcModule.MouseLook.CurrentHorizontal = rotationX;
            fpcRole.FpcModule.MouseLook.ApplySyncValues((ushort)rotationY, (ushort)rotationX);
        }

        public static void ForceState(ReferenceHub hub, PlayerMovementState playerMovementState)
        {
            if (hub is null)
                return;

            if (hub.roleManager is null)
                return;

            if (hub.roleManager.CurrentRole is null)
                return;

            if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
                return;

            if (fpcRole is null)
                return;

            if (fpcRole.FpcModule is null)
                return;

            fpcRole.FpcModule.CurrentMovementState = playerMovementState;
            fpcRole.FpcModule.StateProcessor?.UpdateMovementState(playerMovementState);
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
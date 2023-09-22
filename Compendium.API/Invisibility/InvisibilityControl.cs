using Mirror;

using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Visibility;

using System.Collections.Generic;

using helpers;
using helpers.Patching;

using Compendium.Round;

namespace Compendium.Invisibility
{
    public static class InvisibilityControl
    {
        private static readonly Dictionary<uint, List<uint>> _invisMatrix = new Dictionary<uint, List<uint>>();
        private static readonly HashSet<uint> _invisList = new HashSet<uint>();

        public static bool IsInvisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisList.Contains(player.netId))
                return true;

            return _invisMatrix.TryGetValue(player.netId, out var list) && list.Contains(target.netId);
        }

        public static bool IsInvisible(this ReferenceHub player)
            => _invisList.Contains(player.netId);

        public static void MakeInvisible(this ReferenceHub player)
            => _invisList.Add(player.netId);

        public static void MakeVisible(this ReferenceHub player)
            => _invisList.Remove(player.netId);

        public static void MakeInvisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisMatrix.TryGetValue(player.netId, out var list))
                list.Add(target.netId);
            else
                _invisMatrix[player.netId] = new List<uint>() { target.netId };
        }

        public static void MakeVisibleTo(this ReferenceHub player, ReferenceHub target)
        {
            if (_invisMatrix.TryGetValue(player.netId, out var list))
                list.Remove(target.netId);
        }

        [RoundStateChanged(RoundState.WaitingForPlayers)]
        private static void OnWait()
        {
            _invisList.Clear();
            _invisMatrix.Clear();
        }

        [Patch(typeof(FpcServerPositionDistributor), nameof(FpcServerPositionDistributor.WriteAll), PatchType.Prefix)]
        private static bool InvisPatch(ReferenceHub receiver, NetworkWriter writer)
        {
            var index = ushort.MinValue;
            var visRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
            var hasRole = false;

            VisibilityController controller = null;

            if (visRole != null)
            {
                hasRole = true;
                controller = visRole.VisibilityController;
            }
            else
            {
                hasRole = false;
                controller = null;
            }

            ReferenceHub.AllHubs.ForEach(hub =>
            {
                if (hub.netId != receiver.netId)
                {
                    if (hub.Role() is IFpcRole fpcRole)
                    {
                        var isInvisible = hasRole && !controller.ValidateVisibility(hub);

                        if (!isInvisible)
                        {
                            if (_invisList.Contains(hub.netId))
                                isInvisible = true;
                            else if (_invisMatrix.TryGetValue(hub.netId, out var invisList) && invisList.Contains(receiver.netId))
                                isInvisible = true;
                        }

                        var syncData = FpcServerPositionDistributor.GetNewSyncData(receiver, hub, fpcRole.FpcModule, isInvisible);

                        if (!isInvisible)
                        {
                            FpcServerPositionDistributor._bufferPlayerIDs[index] = hub.PlayerId;
                            FpcServerPositionDistributor._bufferSyncData[index] = syncData;

                            index++;
                        }
                    }
                }
            });

            writer.WriteUShort(index);

            for (int i = 0; i < index; i++)
            {
                writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[i]));
                FpcServerPositionDistributor._bufferSyncData[i].Write(writer);
            }

            return false;
        }
    }
}

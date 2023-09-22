using Compendium.Snapshots.Capture;

using helpers;

using System.Collections.Generic;

namespace Compendium.Snapshots
{
    public static class SnapshotManager
    {
        private static readonly Dictionary<uint, List<Snapshot>> _snapshots = new Dictionary<uint, List<Snapshot>>();
        private static readonly HashSet<ISnapshotCapture> _captures = new HashSet<ISnapshotCapture>()
        {
            new RoleDataCapture()
        };

        public static bool TryGetCapture(SnapshotDataType snapshotDataType, out ISnapshotCapture capture)
            => _captures.TryGetFirst(d => d.Type == snapshotDataType, out capture);

        public static bool TryGetSnapshots(ReferenceHub player, out List<Snapshot> snapshots)
            => _snapshots.TryGetValue(player.netId, out snapshots);

        public static Snapshot CaptureAll(ReferenceHub hub)
            => Capture(hub, SnapshotDataType.Inventory, SnapshotDataType.Stats, SnapshotDataType.Role);

        public static Snapshot Capture(ReferenceHub hub, params SnapshotDataType[] includedData)
        {
            var ss = Snapshot.Capture(hub, includedData);

            if (_snapshots.TryGetValue(hub.netId, out var snapshots))
                snapshots.Add(ss);
            else
                _snapshots.Add(hub.netId, new List<Snapshot>() { ss });

            return ss;
        }
    }
}

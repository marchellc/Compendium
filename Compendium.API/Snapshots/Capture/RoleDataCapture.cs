using Compendium.Snapshots.Data;

namespace Compendium.Snapshots.Capture
{
    public class RoleDataCapture : ISnapshotCapture
    {
        public SnapshotDataType Type => SnapshotDataType.Role;

        public ISnapshotData Capture(ReferenceHub hub)
            => new RoleData(hub);
    }
}
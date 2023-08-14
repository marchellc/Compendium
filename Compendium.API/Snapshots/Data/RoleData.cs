using helpers.Time;

using PlayerRoles;

using System;

namespace Compendium.Snapshots.Data
{
    public class RoleData : ISnapshotData
    {
        private ReferenceHub _restoreTarget;

        public RoleTypeId Role { get; }
        public DateTime Time { get; }

        public SnapshotDataType Type => SnapshotDataType.Role;

        public RoleData(ReferenceHub player)
        {
            Role = player.RoleId();
            Time = TimeUtils.LocalTime;
        }

        public void Apply(ReferenceHub target)
        {
            if (target.RoleId() != Role)
                target.RoleId(Role);

            _restoreTarget = target;
            Calls.Delay(0.2f, InternalRestore);
        }

        private void InternalRestore()
        {


            _restoreTarget = null;
        }
    }
}

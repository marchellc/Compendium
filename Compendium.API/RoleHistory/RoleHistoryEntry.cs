using Compendium.Snapshots;
using PlayerRoles;

using System;

namespace Compendium.RoleHistory
{
    public class RoleHistoryEntry
    {
        public PlayerSnapshot Snapshot { get; set; }
        public RoleChangeReason Reason { get; set; }

        public DateTime Time { get; set; }
    }
}
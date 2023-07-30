using Compendium.Snapshots;
using PlayerRoles;

using System;

namespace Compendium.RoleHistory
{
    public class RoleHistoryEntry
    {
        public RoleTypeId Previous { get; set; }
        public RoleTypeId New { get; set; }

        public RoleChangeReason Reason { get; set; }

        public InventorySnapshot Inventory { get; set; }

        public float Health { get; set; }

        public DateTime Time { get; set; }
    }
}
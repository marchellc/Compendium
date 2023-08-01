namespace Compendium.Snapshots
{
    public static class SnapshotHelper
    {
        public static InventorySnapshot SaveInventory(this ReferenceHub hub)
            => new InventorySnapshot(hub);

        public static RoleSnapshot SaveRole(this ReferenceHub hub)
            => new RoleSnapshot(hub);

        public static PlayerSnapshot Save(this ReferenceHub hub)
            => new PlayerSnapshot(hub);
    }
}
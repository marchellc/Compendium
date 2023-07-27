namespace Compendium.Helpers.Snapshots
{
    public static class SnapshotHelper
    {
        public static InventorySnapshot SaveInventory(this ReferenceHub hub)
            => new InventorySnapshot(hub);
    }
}
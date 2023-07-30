using helpers.Extensions;

using InventorySystem;

using System.Collections.Generic;

namespace Compendium.Snapshots
{
    public struct InventorySnapshot
    {
        private Dictionary<ItemType, ushort> _ammo;
        private List<ItemType> _inv;

        public IReadOnlyDictionary<ItemType, ushort> Ammo => _ammo;
        public IReadOnlyList<ItemType> Inventory => _inv;

        public InventorySnapshot(ReferenceHub hub)
        {
            _ammo = new Dictionary<ItemType, ushort>();
            _inv = new List<ItemType>();

            foreach (var p in hub.inventory.UserInventory.ReserveAmmo)
                _ammo[p.Key] = p.Value;

            foreach (var i in hub.inventory.UserInventory.Items)
                _inv.Add(i.Value.ItemTypeId);
        }

        public void Restore(ReferenceHub hub)
        {
            foreach (var i in hub.inventory.UserInventory.Items)
                hub.inventory.ServerRemoveItem(i.Key, i.Value.PickupDropModel);

            hub.inventory.UserInventory.Items.Clear();
            hub.inventory.UserInventory.ReserveAmmo.Clear();
            hub.inventory.UserInventory.ReserveAmmo.AddRange(_ammo);

            foreach (var item in _inv)
                hub.inventory.ServerAddItem(item);

            hub.inventory.SendAmmoNextFrame = true;
            hub.inventory.SendItemsNextFrame = true;
        }
    }
}
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using System.Collections.Generic;

namespace Compendium.Common.CustomItems
{
    public static class CustomItemManager
    {
        private static Dictionary<ushort, ICustomItem> m_ItemTracker = new Dictionary<ushort, ICustomItem>();

        public static bool TryGetItem(this ItemBase item, out ICustomItem customItem) => m_ItemTracker.TryGetValue(item.ItemSerial, out customItem);
        public static bool TryGetItem(this ItemPickupBase pickup, out ICustomItem customItem) => m_ItemTracker.TryGetValue(pickup.NetworkInfo.Serial, out customItem);

        public static bool IsCustomItem(this ItemBase item) => TryGetItem(item, out _);
        public static bool IsCustomItem(this ItemPickupBase pickup) => TryGetItem(pickup, out _);
    }
}
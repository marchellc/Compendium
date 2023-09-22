using InventorySystem.Items;
using InventorySystem.Items.Pickups;

namespace Compendium.Custom.Items
{
    public class CustomItemHandler<TItem, TPickup> : CustomItemHandlerBase
        where TItem : ItemBase
        where TPickup : ItemPickupBase
    {
        public new TItem Item { get; }
        public new TPickup Pickup { get; }
    }
}
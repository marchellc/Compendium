using helpers;

using InventorySystem.Items.Pickups;

namespace Compendium.Items
{
    public class CustomPickupHandler<TPickup> : CustomPickupHandlerBase
    {
        private TPickup _item;

        public new TPickup Item { get; }

        internal override void SetPickup(ItemPickupBase item)
        {
            base.SetPickup(item);
            item?.Is(out _item);
        }
    }
}
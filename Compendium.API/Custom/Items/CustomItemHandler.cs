using helpers;

using InventorySystem.Items;

namespace Compendium.Custom.Items
{
    public class CustomItemHandler<TItem> : CustomItemHandlerBase
        where TItem : ItemBase
    {
        private TItem _item;

        public new TItem Item => _item;

        internal override void SetItem(ItemBase item)
        {
            base.SetItem(item);
            item?.Is(out _item);
        }
    }
}
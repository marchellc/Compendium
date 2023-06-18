using InventorySystem.Items;
using InventorySystem.Items.Pickups;

namespace Compendium.Common.CustomItems
{
    public interface ICustomItem
    {
        string Name { get; }

        int Id { get; }

        bool IsSelected { get; }

        ItemType Type { get; }
        CustomItemStatus Status { get; }

        ItemBase Item { get; }
        ItemPickupBase Pickup { get; }

        ReferenceHub Owner { get; }

        ICustomItem Instantiate();

        bool CanAdd(ReferenceHub target);
        void OnAdded();

        bool CanRemove();
        void OnRemoved();

        void OnDeselected();
        void OnDeselecting();

        void OnSelected();
        void OnSelecting();
    }
}

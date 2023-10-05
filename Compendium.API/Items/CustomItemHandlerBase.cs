using Compendium.Extensions;

using helpers;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using UnityEngine;

namespace Compendium.Items
{
    public class CustomItemHandlerBase
    {
        private CustomItemBase _customItem;

        public ushort Serial { get; set; }

        public ItemBase Item { get; }

        public ReferenceHub Owner
        {
            get => Item != null ? Item.Owner : null;
            set
            {
                if (value is null)
                    DestroyItem();
                else
                {
                    if (Owner != null
                        && value == Owner)
                        return;

                    Give(value, false);
                }
            }
        }

        public CustomItemBase CustomItem
        {
            get => _customItem;
        }

        public bool IsOwned
        {
            get => Item != null && Item.Owner != null;
        }

        public bool IsWorn
        {
            get => Item != null && Item is IWearableItem wearable && wearable.IsWorn;
        }

        public bool IsSelected
        {
            get => Item != null && Item.Owner != null && Item.OwnerInventory.CurInstance != null && Item.OwnerInventory.CurInstance == Item;
        }

        public bool IsEquipped
        {
            get => Item?.IsEquipped ?? false;
        }

        public float Weight
        {
            get => Item is null ? 0f : Item.Weight;
            set => CustomItemOverrides.SetWeightOverride(Item, value, false);
        }

        public int InventoryIndex
        {
            get => Owner is null ? -1 : Owner.inventory.UserInventory.Items.FindKeyIndex(Serial);
            set
            {
                if (Owner is null)
                    return;

                if (value < 1 || value >= 8)
                    return;

                Owner.inventory.UserInventory.Items.SetIndex(value, Serial, Item);
                Owner.inventory.SendItemsNextFrame = true;
            }
        }

        public ItemType Type
        {
            get => Item?.ItemTypeId ?? ItemType.None;
            set
            {
                var owner = Owner;

                DestroyItem(false);

                var item = ReferenceHub.HostHub.inventory.CreateItemInstance(new ItemIdentifier(value, Serial), false);

                if (item is null)
                    return;

                if (owner != null)
                {
                    owner.inventory.UserInventory.Items[Serial] = item;
                    item.OnAdded(null);
                    owner.inventory.SendItemsNextFrame = true;
                }

                SetItem(item);
            }
        }

        public bool IsOwnedBy(ReferenceHub hub)
            => Owner != null && Owner == hub;

        public void Give(ReferenceHub target, bool dropIfFull, bool removeInventory = true)
        {
            if (CustomItem is null)
                return;

            if (Item != null)
                DestroyItem(removeInventory);

            if (target.inventory.UserInventory.Items.Count >= 8 
                && !CustomItem.InventoryType.IsAmmo())
            {
                if (dropIfFull)
                {
                    CustomItem?.Spawn(target.Position(), target.Rotation());
                    Destroy();
                }
            }
            else
            {
                var item = CustomItem?.CreateItem(Serial);

                if (item is null)
                    return;

                target.inventory.UserInventory.Items[Serial] = item;
                item.OnAdded(null);
                target.inventory.SendItemsNextFrame = true;

                SetItem(item);
            }
        }

        public void Remove()
        {
            if (Owner is null)
                return;

            DestroyItem();
        }

        public void Destroy()
        {
            if (Item != null)
            {
                if (Item.Owner != null)
                    Item.OwnerInventory.ServerRemoveItem(Serial, Item.PickupDropModel);

                DestroyItem();
            }

            if (CustomItem != null)
                DestroyCustomItem();

            OnDestroy();
        }

        public CustomPickupHandlerBase Drop(Vector3? pos = null, Quaternion? rot = null)
        {
            CustomPickupHandlerBase handler = null;

            if (CustomItem != null && CustomItem.PickupType != ItemType.None)
            {
                if (Owner is null)
                    return null;

                handler = CustomItem?.Spawn(
                    pos.HasValue ? pos.Value : Owner.Position(),
                    rot.HasValue ? rot.Value : Owner.Rotation());
            }

            Destroy();
            return handler;
        }

        internal virtual void SetItem(ItemBase item)
        {
            OnItemSet(item);
        }

        internal virtual void DestroyItem(bool removeInventory = true)
        {
            OnItemDestroyed();

            if (Item != null)
            {
                if (removeInventory)
                {
                    if (Owner != null)
                    {
                        Owner.inventory.ServerRemoveItem(Serial, Item.PickupDropModel);
                        return;
                    }
                }

                Item.OnRemoved(Item.PickupDropModel);
                Object.Destroy(Item.gameObject);

                if (Item != null)
                    SetItem(null);
            }
        }

        internal virtual void DestroyCustomItem()
        {
            OnCustomItemDestroyed();
            _customItem = null;
        }

        public virtual void OnCreated(CustomItemBase item) { }
        public virtual void OnItemSet(ItemBase item) { }

        public virtual void OnItemDestroyed() { }
        public virtual void OnCustomItemDestroyed() { }

        public virtual bool OnAdding(ReferenceHub target) => true;
        public virtual void OnAdded() { }

        public virtual bool OnRemoving() => true;
        public virtual void OnRemoved() { }

        public virtual bool OnSelecting(ItemBase current) => true;
        public virtual void OnSelected() { }

        public virtual bool OnDeselecting(ItemBase next) => true;
        public virtual void OnDeselected() { }

        public virtual void OnDestroy() { }
    }
}
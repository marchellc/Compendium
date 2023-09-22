using Compendium.Extensions;
using helpers;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using UnityEngine;

namespace Compendium.Custom.Items
{
    public class CustomItemHandlerBase
    {
        internal CustomItemBase _cItem;

        public ushort Serial { get; set; }

        public ItemBase Item { get; }
        public ItemPickupBase Pickup { get; }

        public CustomItemHandlerBase()
        {
            Serial = ItemSerialGenerator.GenerateNext();
        }

        public CustomItemBase CustomItem
        {
            get => _cItem;
        }

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

        public bool IsOwned
        {
            get => Item != null && Item.Owner != null;
        }

        public bool IsSpawned
        {
            get => Pickup != null;
        }

        public bool IsWorn
        {
            get => Item != null && Item is IWearableItem wearable && wearable.IsWorn;
        }

        public bool IsSelected
        {
            get => Item != null && Item.Owner != null && Item.OwnerInventory.CurInstance != null && Item.OwnerInventory.CurInstance == Item;
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

        public void Drop(ItemType? pickupTypeOverride = null)
        {
            if (Item is null
                || Item.Owner is null
                || Item.PickupDropModel is null)
                return;

            var pickupInfo = new PickupSyncInfo(pickupTypeOverride.HasValue ? pickupTypeOverride.Value : Item.ItemTypeId, Item.Weight, Item.ItemSerial);
            var pickup = Item.Owner.inventory.ServerCreatePickup(Item, pickupInfo, true);

            pickup.PreviousOwner = new Footprinting.Footprint(Item.Owner);

            SetPickup(pickup);
        }

        public bool IsOwnedBy(ReferenceHub hub)
            => Owner != null && Owner == hub;

        public void Spawn(ItemType itemId, Vector3 position, Quaternion rotation)
        {
            if (!InventoryItemLoader.TryGetItem<ItemBase>(itemId, out var item))
                return;

            var pickupInfo = new PickupSyncInfo(itemId, item.Weight, Serial);
            var pickup = InventorySystem.InventoryExtensions.ServerCreatePickup(item, pickupInfo, position, rotation, true);

            if (Item != null)
                DestroyItem();

            SetPickup(pickup);
        }

        public void Give(ReferenceHub target, bool dropIfFull)
        {
            if (CustomItem is null)
                return;

            if (Item != null)
                DestroyItem();

            if (target.inventory.UserInventory.Items.Count >= 8 
                && !CustomItem.InventoryType.IsAmmo())
            {
                if (dropIfFull)
                    Spawn(CustomItem.InventoryType, target.Position(), target.Rotation());
            }
            else
            {
                var item = target.inventory.CreateItemInstance(new ItemIdentifier(CustomItem.InventoryType, Serial), target.isLocalPlayer);

                if (item is null)
                    return;

                target.inventory.UserInventory.Items[Serial] = item;

                item.OnAdded(null);

                target.inventory.SendItemsNextFrame = true;

                SetItem(item);

                if (Pickup != null)
                    DestroyPickup();
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

            if (Pickup != null)
                DestroyPickup();

            OnDestroy();

            _cItem?.OnHandlerDestroyed(this);
            _cItem = null;
        }

        internal virtual void SetItem(ItemBase item)
        {
            OnItemSet(item);
        }

        internal virtual void SetPickup(ItemPickupBase item)
        {
            OnPickupSet(item);
        }

        internal virtual void DestroyItem()
        {
            OnItemDestroyed();

            if (Item != null)
            {
                if (Owner != null)
                {
                    Owner.inventory.ServerRemoveItem(Serial, Item.PickupDropModel);
                    return;
                }

                Item.OnRemoved(Item.PickupDropModel);
                Object.Destroy(Item.gameObject);             
            }
        }

        internal virtual void DestroyPickup()
        {
            OnPickupDestroyed();

            if (Pickup != null)
                Pickup.DestroySelf();
        }

        internal virtual void OnCreated(CustomItemBase item)
        {
            _cItem = item;
        }

        internal virtual void OnItemSet(ItemBase item) { }
        internal virtual void OnPickupSet(ItemPickupBase item) { }

        internal virtual void OnItemDestroyed() { }
        internal virtual void OnPickupDestroyed() { }

        public virtual bool OnSpawning(ref Vector3 position, ref PickupSyncInfo info) => true;
        public virtual void OnSpawned(Vector3 position, PickupSyncInfo info) { }

        public virtual void OnDespawning() { }
        public virtual void OnDespawned() { }

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
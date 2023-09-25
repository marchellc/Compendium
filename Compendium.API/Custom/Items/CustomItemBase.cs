using helpers;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using Mirror;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Compendium.Custom.Items
{
    public class CustomItemBase
    {
        private readonly HashSet<CustomItemHandlerBase> _itemHandlers = new HashSet<CustomItemHandlerBase>();
        private readonly HashSet<CustomPickupHandlerBase> _pickupHandlers = new HashSet<CustomPickupHandlerBase>();

        public virtual string Name { get; } = "default";
        public virtual ushort Id { get; } = 100;

        public virtual ItemType InventoryType { get; } = ItemType.None;
        public virtual ItemType PickupType { get; } = ItemType.None;

        public IEnumerable<CustomItemHandlerBase> ItemHandlers => _itemHandlers;
        public IEnumerable<CustomPickupHandlerBase> PickupHandlers => _pickupHandlers;

        public IEnumerable<ReferenceHub> Owners => ItemHandlers.Select(x => x.Owner).Where(x => x != null);
        public IEnumerable<ItemBase> Items => ItemHandlers.Select(x => x.Item).Where(x => x != null);
        public IEnumerable<ItemPickupBase> Pickups => PickupHandlers?.Select(x => x.Item).Where(x => x != null);

        public CustomItemHandlerBase Give(ReferenceHub target, bool dropIfFull = false)
        {
            var handler = CreateItemHandler(ItemSerialGenerator.GenerateNext());

            if (handler is null)
                return null;

            handler.Give(target, dropIfFull);
            return handler;
        }

        public CustomPickupHandlerBase Spawn(Vector3 position, Quaternion rotation)
        {
            var pickup = CreatePickup(ItemSerialGenerator.GenerateNext());

            if (pickup is null)
                return null;

            pickup.Position = position;
            pickup.Rotation = rotation;

            NetworkServer.Spawn(pickup.gameObject);

            var pickupHandler = CreatePickupHandler(pickup.Info.Serial);

            pickupHandler.SetPickup(pickup);
            return pickupHandler;
        }

        public void Remove(params ReferenceHub[] targets)
        {
            targets.ForEach(hub =>
            {
                if (!TryGetItemHandler(hub, out var handler))
                    return;

                handler.Remove();
            });
        }

        public void Drop(params ReferenceHub[] targets)
        {
            targets.ForEach(hub =>
            {
                if (!TryGetItemHandler(hub, out var handler))
                    return;

                handler.Drop();
            });
        }

        public void Destroy() 
        {
            ItemHandlers.ForEach(handler =>
            {
                handler.Destroy();
            });

            PickupHandlers.ForEach(handler =>
            {
                handler.Destroy();
            });
        }

        public void RemoveAll()
        {
            ItemHandlers.ForEach(handler =>
            {
                handler.Remove();
            });
        }

        public void DropAll()
        {
            ItemHandlers.ForEach(handler =>
            {
                handler.Drop();
            });
        }

        public bool TryGetItemHandler(ReferenceHub hub, out CustomItemHandlerBase handler)
            => _itemHandlers.TryGetFirst(h => h.Owner != null && h.Owner == hub, out handler);

        public bool TryGetItemHandler(ushort serial, out CustomItemHandlerBase handler)
            => _itemHandlers.TryGetFirst(h => h.Serial == serial, out handler);

        public bool TryGetPickupHandler(ushort serial, out CustomPickupHandlerBase handler)
            => _pickupHandlers.TryGetFirst(h => h.Serial == serial, out handler);

        internal virtual void OnItemHandlerCreated(CustomItemHandlerBase customItem) { }
        internal virtual void OnPickupHandlerCreated(CustomPickupHandlerBase customPickup) { }

        internal virtual void OnItemHandlerDestroyed(CustomItemHandlerBase customItem)
        {
            _itemHandlers.Remove(customItem);
        }

        internal virtual void OnPickupHandlerDestroyed(CustomPickupHandlerBase customPickup)
        {
            _pickupHandlers.Remove(customPickup);
        }

        internal virtual void ClearHandlers() { }

        internal virtual CustomItemHandlerBase CreateItemHandler(ushort serial) { return null; }
        internal virtual CustomPickupHandlerBase CreatePickupHandler(ushort serial) { return null; }

        internal virtual ItemPickupBase CreatePickup(ushort serial)
        {
            if (!InventoryItemLoader.TryGetItem<ItemBase>(PickupType, out var itemPrefab)
                || itemPrefab.PickupDropModel is null)
                return null;

            var pickup = Object.Instantiate(itemPrefab.PickupDropModel);

            pickup.Info = new PickupSyncInfo(PickupType, itemPrefab.Weight, serial);

            SetupPickup(pickup);
            return pickup;
        }

        internal virtual ItemBase CreateItem(ushort serial)
        {
            var item = ReferenceHub.HostHub.inventory.CreateItemInstance(new ItemIdentifier(InventoryType, serial), true);
            SetupItem(item);
            return item;
        }

        internal virtual void SetupPickup(ItemPickupBase item) { }
        internal virtual void SetupItem(ItemBase item) { }

        internal virtual void SetupItemHandler(CustomItemHandlerBase handler, ushort? serialOverride)
        {
            _itemHandlers.Add(handler);

            if (serialOverride.HasValue)
                handler.Serial = serialOverride.Value;
            else if (handler.Serial <= 0)
                handler.Serial = ItemSerialGenerator.GenerateNext();

            handler.OnCreated(this);
            OnItemHandlerCreated(handler);
        }

        internal virtual void SetupPickupHandler(CustomPickupHandlerBase handler, ushort? serialOverride)
        {
            _pickupHandlers.Add(handler);

            if (serialOverride.HasValue)
                handler.Serial = serialOverride.Value;
            else if (handler.Serial <= 0)
                handler.Serial = ItemSerialGenerator.GenerateNext();

            handler.OnCreated(this);
            OnPickupHandlerCreated(handler);
        }
    }
}
using helpers;

using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Custom.Items
{
    public class CustomItemBase
    {
        public virtual string Name { get; }
        public virtual short Id { get; }

        public virtual ItemType InventoryType { get; } 

        public virtual IReadOnlyCollection<CustomItemHandlerBase> Handlers { get; }

        public IReadOnlyCollection<ReferenceHub> Owners
        {
            get
            {
                if (Handlers is null || !Handlers.Any())
                    return null;

                var set = new HashSet<ReferenceHub>();

                Handlers.ForEach(handler =>
                {
                    if (handler.Item != null && handler.Item.Owner != null)
                        set.Add(handler.Item.Owner);
                });

                return set;
            }
        }

        public IReadOnlyCollection<ItemBase> Items
        {
            get
            {
                if (Handlers is null || !Handlers.Any())
                    return null;

                var set = new HashSet<ItemBase>();

                Handlers.ForEach(handler =>
                {
                    if (handler.Item != null)
                        set.Add(handler.Item);
                });

                return set;
            }
        }

        public IReadOnlyCollection<ItemPickupBase> Pickups
        {
            get
            {
                if (Handlers is null || !Handlers.Any())
                    return null;

                var set = new HashSet<ItemPickupBase>();

                Handlers.ForEach(handler =>
                {
                    if (handler.Pickup != null)
                        set.Add(handler.Pickup);
                });

                return set;
            }
        }

        public void Give(bool dropIfFull, params ReferenceHub[] targets)
        {
            targets.ForEach(hub =>
            {
                var handler = CreateHandler();

                if (handler is null)
                    return;

                handler.Give(hub, dropIfFull);
            });
        }

        public void Remove(params ReferenceHub[] targets)
        {
            targets.ForEach(hub =>
            {
                if (!TryGetHandler(hub, out var handler))
                    return;

                handler.Remove();
            });
        }

        public void Drop(ItemType? pickupItemOverride, params ReferenceHub[] targets)
        {
            targets.ForEach(hub =>
            {
                if (!TryGetHandler(hub, out var handler))
                    return;

                handler.Drop(pickupItemOverride);
            });
        }

        public void Destroy() 
        {
            Handlers.ForEach(handler =>
            {
                handler.Destroy();
            });
        }

        public void RemoveAll()
        {
            Handlers.ForEach(handler =>
            {
                handler.Remove();
            });
        }

        public void DropAll(ItemType? pickupItemOverride = null)
        {
            Handlers.ForEach(handler =>
            {
                handler.Drop(pickupItemOverride);
            });
        }

        public bool TryGetHandler(ReferenceHub hub, out CustomItemHandlerBase handler)
            => Handlers.TryGetFirst(h => h.Owner != null && h.Owner == hub, out handler);

        internal virtual void OnHandlerCreated(CustomItemHandlerBase customItem) { }
        internal virtual void OnHandlerDestroyed(CustomItemHandlerBase customItem) { }

        internal virtual void ClearHandlers() { }

        internal virtual CustomItemHandlerBase CreateHandler() { return null; }

        internal virtual void SetupHandler(CustomItemHandlerBase handler)
        {
            handler.OnCreated(this);
            OnHandlerCreated(handler);
        }
    }
}
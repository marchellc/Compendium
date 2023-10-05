using Compendium.Extensions;

using helpers;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Items
{
    public static class CustomItemManager
    {
        private static readonly HashSet<CustomItemBase> _customItems = new HashSet<CustomItemBase>();

        public static bool TryUnregisterItem(Type type)
        {
            if (!TryGetCustomItem(type, out var item))
                return false;

            _customItems.Remove(item);
            Plugin.Info($"Unregistered custom item ");
            return true;
        }

        public static void TryRegisterItems()
            => TryRegisterItems(Assembly.GetCallingAssembly());

        public static void TryRegisterItems(Assembly assembly)
            => assembly.ForEachType(type =>
            {
                if (!Reflection.HasType<CustomItemBase>(type))
                    return;

                TryRegisterItem(type, true);
            });

        public static bool TryRegisterItem<TItem>() where TItem : CustomItemBase, new()
            => TryRegisterItem(typeof(TItem));

        public static bool TryRegisterItem(Type type, bool isValidated = false)
        {
            if (!isValidated && !Reflection.HasType<CustomItemBase>(type))
            {
                Plugin.Error($"Attempted to register a type that does not inherit the CustomItemBase class: '{type.FullName}'");
                return false;
            }

            var instance = Reflection.Instantiate<CustomItemBase>();

            if (instance is null)
            {
                Plugin.Error($"Failed to register custom item of type '{type.FullName}': failed to create class instance");
                return false;
            }

            return TryRegisterItem(instance);
        }

        public static bool TryRegisterItem(CustomItemBase item)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                Plugin.Error($"Failed to register custom item '{item.Name}' ({item.Id}) as it's name is empty.");
                return false;
            }

            if (item.Id < 100)
            {
                Plugin.Error($"Failed to register custom item '{item.Name}' ({item.Id}) as it's ID is lower than 100.");
                return false;
            }

            if (item.InventoryType == ItemType.None)
            {
                Plugin.Error($"Failed to register custom item '{item.Name}' ({item.Id}) as it's inventory type is set to None.");
                return false;
            }

            if (TryGetCustomItem(item.Id, out CustomItemBase reference))
            {
                Plugin.Error($"Failed to register custom item '{item.Name}' ({item.Id}) as there's already an item with the same ID: '{reference.Name}'");
                return false;
            }

            if (TryGetCustomItem(item.Name, out reference))
            {
                Plugin.Error($"Failed to register custom item '{item.Name}' ({item.Id}) as there's already an item with a similar name: '{reference.Name}'");
                return false;
            }

            _customItems.Add(item);
            Plugin.Info($"Registered custom item '{item.Name}' ({item.Id}) (inventory type: {item.InventoryType.GetName()}; pickup type: {item.PickupType.GetName()}; class: {item.GetType().FullName})");
            return true;
        }

        public static bool TryGetCustomItem(ushort id, out CustomItemBase item)
            => _customItems.TryGetFirst(i => i.Id == id, out item);

        public static bool TryGetCustomItem(string name, out CustomItemBase item)
            => _customItems.TryGetFirst(i => i.Name.GetSimilarity(name) >= 0.8, out item);

        public static bool TryGetCustomItem(Type type, out CustomItemBase item)
            => _customItems.TryGetFirst(i => i.GetType() == type, out item);

        public static bool TryGetCustomItem<TItem>(out TItem item) where TItem : CustomItemBase
        {
            if (_customItems.TryGetFirst(i => i is TItem, out var customItem) && customItem.Is(out item))
                return true;

            item = default;
            return false;
        }

        public static bool TryGetCustomItem<THandler>(ushort serial, out THandler handler) where THandler : CustomItemHandlerBase
        {
            foreach (var item in _customItems)
            {
                if (item.TryGetItemHandler(serial, out var customItem) && customItem.Is(out handler))
                    return true;
            }

            handler = null;
            return false;
        }

        public static bool TryGetCustomItem(ushort serial, out CustomItemHandlerBase customItem)
        {
            foreach (var item in _customItems)
            {
                if (item.TryGetItemHandler(serial, out customItem))
                    return true;
            }

            customItem = null;
            return false;
        }

        public static bool TryGetCustomPickup<THandler>(ushort serial, out THandler handler) where THandler : CustomPickupHandlerBase
        {
            foreach (var item in _customItems)
            {
                if (item.TryGetPickupHandler(serial, out var customPickup) && customPickup.Is(out handler))
                    return true;
            }

            handler = null;
            return false;
        }

        public static bool TryGetCustomPickup(ushort serial, out CustomPickupHandlerBase customPickup)
        {
            foreach (var item in _customItems)
            {
                if (item.TryGetPickupHandler(serial, out customPickup))
                    return true;
            }

            customPickup = null;
            return false;
        }

        public static bool TryGetCustomPickups<THandler>(out THandler[] customPickups)
        {
            var list = Pools.PoolList<THandler>();

            _customItems.For((_, item) =>
            {
                item.PickupHandlers.For((_, pickup) =>
                {
                    if (!pickup.Is<THandler>(out var handler))
                        return;

                    list.Add(handler);
                });
            });

            customPickups = list.ToArray();
            list.ReturnList();
            return customPickups.Any();
        }

        public static bool TryGetCustomPickups(out CustomPickupHandlerBase[] customPickups)
        {
            var list = Pools.PoolList<CustomPickupHandlerBase>();

            _customItems.For((_, item) =>
            {
                item.PickupHandlers.For((_, pickup) =>
                {
                    list.Add(pickup);
                });
            });

            customPickups = list.ToArray();
            list.ReturnList();
            return customPickups.Any();
        }

        public static bool TryGetCustomItems(ItemType item, out CustomItemBase[] items)
        {
            var list = Pools.PoolList<CustomItemBase>();

            _customItems.For((_, it) =>
            {
                if (it.InventoryType == item)
                    list.Add(it);
            });

            items = list.ToArray();
            list.ReturnList();
            return items.Any();
        }

        public static bool TryGetCustomItems<THandler>(ReferenceHub hub, out THandler[] items)
        {
            if (!hub.inventory.UserInventory.Items.Any())
            {
                items = Array.Empty<THandler>();
                return false;
            }

            var list = Pools.PoolList<THandler>();

            hub.inventory.UserInventory.Items.For((_, pair) =>
            {
                _customItems.For((_, item) =>
                {
                    if (item.TryGetItemHandler(pair.Key, out var handler)
                        && handler.Is<THandler>(out var cHandler))
                        list.Add(cHandler);
                });
            });

            items = list.ToArray();
            list.ReturnList();
            return items.Any();
        }

        public static bool TryGetCustomItems(ReferenceHub hub, out CustomItemHandlerBase[] items)
        {
            if (!hub.inventory.UserInventory.Items.Any())
            {
                items = Array.Empty<CustomItemHandlerBase>();
                return false;
            }

            var list = Pools.PoolList<CustomItemHandlerBase>();

            hub.inventory.UserInventory.Items.For((_, pair) =>
            {
                _customItems.For((_, item) =>
                {
                    if (item.TryGetItemHandler(pair.Key, out var handler))
                        list.Add(handler);
                });
            });

            items = list.ToArray();
            list.ReturnList();
            return items.Any();
        }
    }
}

using Compendium.Extensions;
using Compendium.Hints;

using helpers.Extensions;

using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using MapGeneration.Distributors;

using Mirror;

using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Compendium
{
    public static class World
    {
        public static Vector3 EscapePosition => Escape.WorldPos;

        public static IEnumerable<ItemPickupBase> Pickups => Object.FindObjectsOfType<ItemPickupBase>();
        public static IEnumerable<ItemBase> Items => Hub.Hubs.SelectMany(hub => hub.GetItems());
        public static IEnumerable<BasicRagdoll> Ragdolls => Object.FindObjectsOfType<BasicRagdoll>();
        public static IEnumerable<DoorVariant> Doors => DoorVariant.AllDoors;
        public static IEnumerable<DoorVariant> Gates => Doors.Where(d => d.IsGate());
        public static IEnumerable<ElevatorChamber> Elevators => Object.FindObjectsOfType<ElevatorChamber>();
        public static IEnumerable<Scp079Camera> Cameras => Scp079InteractableBase.AllInstances.Where<Scp079Camera>();
        public static IEnumerable<Scp079Generator> Generators => Object.FindObjectsOfType<Scp079Generator>();

        public static bool CanEscape(ReferenceHub hub, bool useGameLogic = true)
            => hub.Position().IsWithinDistance(EscapePosition, Escape.RadiusSqr) && (useGameLogic ? Escape.ServerGetScenario(hub) != Escape.EscapeScenarioType.None : true);

        public static void Broadcast(object message, int duration, bool clear = true)
            => Hub.ForEach(hub => hub.Broadcast(message, duration, clear));

        public static void Hint(object message, float duration, bool clear, params BasicHintEffectType[] effects)
            => Hub.ForEach(hub => hub.Hint(message, duration, clear, effects));

        public static void ClearPickups()
            => Pickups.ForEach(pickup => pickup.DestroySelf());

        public static void ClearPickups(ItemType type)
            => Pickups.Where(p => p.Info.ItemId == type).ForEach(pickup => pickup.DestroySelf());

        public static void ClearItems()
            => Items.ForEach(item => item.OwnerInventory?.ServerRemoveItem(item.ItemSerial, item.PickupDropModel));

        public static void ClearItems(ItemType item)
            => Items.Where(i => i.ItemTypeId == item).ForEach(it => it.OwnerInventory?.ServerRemoveItem(it.ItemSerial, it.PickupDropModel));

        public static void ClearRagdolls()
            => Ragdolls.ForEach(rag => NetworkServer.Destroy(rag.gameObject));

        public static void Clear()
        {
            ClearPickups();
            ClearRagdolls();
        }

        public static List<ItemPickupBase> SpawnItems(ItemType item, Vector3 position, Quaternion rotation, int amount, bool spawn = true)
        {
            var list = new List<ItemPickupBase>();

            for (int i = 0; i < amount; i++)
            {
                var itemObj = SpawnItem(item, position, rotation, spawn);

                if (itemObj != null)
                    list.Add(itemObj);
            }

            return list;
        }

        public static ItemPickupBase SpawnItem(ItemType item, Vector3 position, Quaternion rotation, bool spawn = true)
        {
            if (!InventoryItemLoader.TryGetItem<ItemBase>(item, out var itemBase) || itemBase is null || itemBase.PickupDropModel is null)
                return null;

            var pickup = Object.Instantiate(itemBase.PickupDropModel, position, rotation);

            if (spawn)
                NetworkServer.Spawn(pickup.gameObject);

            return pickup;
        }
    }
}

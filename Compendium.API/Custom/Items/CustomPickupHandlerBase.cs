using Footprinting;

using helpers;

using Interactables.Interobjects;

using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;

using Mirror;

using UnityEngine;

namespace Compendium.Custom.Items
{
    public class CustomPickupHandlerBase
    {
        private CustomItemBase _customItem;

        public ushort Serial { get; set; }

        public ItemPickupBase Item { get; }

        public CustomItemBase CustomItem
        {
            get => _customItem;
        }

        public float Weight
        {
            get => Info.WeightKg;
            set
            {
                Info = new PickupSyncInfo(Info.ItemId, value, Serial);
                StandardPhysics?.UpdateWeight();
            }
        }

        public float BaseSearchTime
        {
            get => Weight + (0.245f + 0.175f);
            set => CustomItemOverrides.SetSearchTimeOverride(Item, value, false);
        }

        public bool IsInUse
        {
            get => Info.InUse;
            set
            {
                var info = Info;
                info.InUse = value;
                Info = info;
            }
        }

        public bool IsLocked
        {
            get => Info.Locked;
            set
            {
                var info = Info;
                info.Locked = value;
                Info = info;
            }
        }

        public bool IsSpawned
        {
            get => Item != null && NetworkServer.spawned.ContainsKey(Item.netId);
            set
            {
                if (value && !IsSpawned)
                    Spawn();
                else if (!value && IsSpawned)
                    Despawn();
            }
        }

        public bool IsFrozen
        {
            get => StandardPhysics?.ServerSendFreeze ?? false;
            set => CustomItemOverrides.SetFrozenOverride(Item, value, !value);
        }

        public bool IsInElevator
        {
            get => Elevator != null;
            set
            {
                if (!value)
                    Elevator = null;
                else
                    throw new System.InvalidOperationException($"You cannot set this property to true.");
            }
        }

        public ElevatorChamber Elevator
        {
            get => StandardPhysics?._trackedChamber ?? null;
            set
            {
                if (StandardPhysics is null)
                    return;

                if (value is null)
                {
                    ParentTransform = null;
                    StandardPhysics._inElevator = false;
                    StandardPhysics._trackedChamber = null;
                }
                else
                {
                    ParentTransform = value.transform;
                    StandardPhysics._trackedChamber = value;
                    StandardPhysics._inElevator = true;
                }
            }
        }

        public GameObject GameObject
        {
            get => Item?.gameObject ?? null;
        }

        public Transform Transform
        {
            get => Item?.transform ?? null;
        }

        public Transform ParentTransform
        {
            get => Item?.transform?.parent ?? null;
            set => Item?.transform?.SetParent(value);
        }

        public Transform CachedTransform
        {
            get => Item is null ? null : Item.CachedTransform;
            set
            {
                if (Item is null)
                    return;

                Item._transform = value;
                Item._transformCacheSet = true;
            }
        }

        public PickupPhysicsModule Physics
        {
            get => Item?.PhysicsModule ?? null;
            set => Item!.PhysicsModule = value;
        }

        public PickupStandardPhysics StandardPhysics
        {
            get => Physics != null && Physics.Is<PickupStandardPhysics>(out var ph) ? ph : null;
            set => Physics = value;
        }

        public Rigidbody Rigidbody
        {
            get => StandardPhysics?.Rb ?? null;
            set => StandardPhysics!.Rb = value;
        }

        public ReferenceHub LastOwnerHub
        {
            get => Item?.PreviousOwner.Hub;
            set => Item!.PreviousOwner = new Footprint(value);
        }

        public ItemType Type
        {
            get => Info.ItemId;
            set
            {
                var pos = Position;
                var rot = Rotation;
                var info = Info;

                info.ItemId = value;

                if (!InventoryItemLoader.TryGetItem<ItemBase>(value, out var itemPrefab)
                    || itemPrefab.PickupDropModel is null)
                    return;

                DestroyPickup();

                var pickup = Object.Instantiate(itemPrefab.PickupDropModel);

                pickup.Position = pos;
                pickup.Rotation = rot;
                pickup.Info = info;

                SetPickup(pickup);
                Spawn();
            }
        }

        public Footprint LastOwner
        {
            get => Item?.PreviousOwner ?? default;
            set => Item!.PreviousOwner = value;
        }

        public PickupSyncInfo Info
        {
            get => Item is null ? default : Item.NetworkInfo;
            set => Item!.NetworkInfo = value;
        }

        public Vector3 Position
        {
            get => Item?.Position ?? Vector3.zero;
            set => Item!.Position = value;
        }

        public Quaternion Rotation
        {
            get => Item?.Rotation ?? Quaternion.identity;
            set => Item!.Rotation = value;
        }

        public void Despawn()
        {
            if (Item is null)
                return;

            NetworkServer.UnSpawn(Item.gameObject);
            OnDespawned();
        }

        public void Spawn(Vector3? pos = null, Quaternion? rot = null)
        {
            if (Item is null)
                return;

            if (pos.HasValue)
                Position = pos.Value;

            if (rot.HasValue)
                Rotation = rot.Value;

            NetworkServer.Spawn(Item.gameObject);
            OnSpawned();
        }

        public void Destroy()
        {
            if (Item != null)
                DestroyPickup();

            if (CustomItem != null)
                DestroyCustomItem();

            OnDestroy();
        }

        public void PickUp(ReferenceHub player)
        {
            if (CustomItem is null)
                return;

            if (CustomItem.Give(player, false) != null)
                DestroyPickup();
        }

        internal virtual void SetPickup(ItemPickupBase item)
        {
            OnPickupSet(item);
        }

        internal virtual void DestroyPickup()
        {
            if (Item != null)
            {
                CustomItemOverrides.SetSearchTimeOverride(Item, 0f, true);
                CustomItemOverrides.SetFrozenOverride(Item, false, true);
            }

            OnDespawned();
            OnPickupDestroyed();

            if (Item != null)
                Item.DestroySelf();
        }

        internal virtual void DestroyCustomItem()
        {
            OnCustomItemDestroyed();
            _customItem = null;
        }

        internal virtual void OnCreated(CustomItemBase item) 
        {
            _customItem = item;
        }

        internal virtual void OnPickupSet(ItemPickupBase item) { }

        internal virtual void OnPickupDestroyed() { }
        internal virtual void OnCustomItemDestroyed() { }

        public virtual void OnSpawned() { }
        public virtual void OnDespawned() { }

        public virtual void OnDestroy() { }

        public virtual void OnShot(ReferenceHub shooter) { }

        // Patch: CollisionDetectionPickup.OnCollisionEnter
        public virtual void OnCollided(Collider collider) { }
    }
}

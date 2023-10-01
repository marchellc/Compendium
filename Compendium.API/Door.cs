using Compendium.Round;

using helpers;
using helpers.Patching;

using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using MapGeneration;

using Mirror;

using PlayerRoles;

using PluginAPI.Events;

using System;
using System.Collections.Generic;

namespace Compendium
{
    public static class Door
    {
        private static Dictionary<uint, KeycardPermissions> _customPerms = new Dictionary<uint, KeycardPermissions>();
        private static Dictionary<uint, Func<DoorVariant, ReferenceHub, bool>> _customAccessModifiers = new Dictionary<uint, Func<DoorVariant, ReferenceHub, bool>>();

        private static Dictionary<uint, List<uint>> _plyWhitelist = new Dictionary<uint, List<uint>>();
        private static Dictionary<uint, List<uint>> _plyBlacklist = new Dictionary<uint, List<uint>>();

        private static HashSet<uint> _disabled = new HashSet<uint>();

        public static IReadOnlyCollection<DoorVariant> Doors => DoorVariant.AllDoors;
        public static IReadOnlyCollection<DoorLockReason> LockTypes { get; } = new DoorLockReason[]
        {
            DoorLockReason.Lockdown079,
            DoorLockReason.Lockdown2176,
            DoorLockReason.DecontLockdown,
            DoorLockReason.Isolation,
            DoorLockReason.Warhead,
            DoorLockReason.Regular079,
            DoorLockReason.AdminCommand,
            DoorLockReason.DecontEvacuate,
            DoorLockReason.NoPower,
            DoorLockReason.SpecialDoorFeature
        };

        public static bool IsOpened(this DoorVariant door)
            => door.TargetState;

        public static bool IsClosed(this DoorVariant door)
            => !door.TargetState;

        public static bool IsDisabled(this DoorVariant door)
            => _disabled.Contains(door.netId);

        public static bool IsDestroyed(this DoorVariant door)
            => door is BreakableDoor breakableDoor && breakableDoor.Network_destroyed;

        public static bool IsGate(this DoorVariant door)
            => door is PryableDoor;

        public static bool IsTimed(this DoorVariant door)
            => door is Timed173PryableDoor;

        public static bool IsBlacklisted(this DoorVariant door, ReferenceHub hub)
            => _plyBlacklist.TryGetValue(door.netId, out var blacklist) && blacklist.Contains(hub.netId);

        public static bool IsWhitelisted(this DoorVariant door, ReferenceHub hub)
            => _plyWhitelist.TryGetValue(door.netId, out var whitelist) && whitelist.Contains(hub.netId);

        public static bool RequiresWhitelist(this DoorVariant door)
            => _plyWhitelist.TryGetValue(door.netId, out var whitelist) && whitelist.Any();

        public static bool HasCustomAccessModifier(this DoorVariant door, out Func<DoorVariant, ReferenceHub, bool> modifier)
            => _customAccessModifiers.TryGetValue(door.netId, out modifier);

        public static bool HasCustomAccessModifier(this DoorVariant door)
            => door.HasCustomAccessModifier(out _);

        public static bool HasCustomPermissions(this DoorVariant door, out KeycardPermissions customPermissions)
            => _customPerms.TryGetValue(door.netId, out customPermissions);

        public static bool HasCustomPermissions(this DoorVariant door)
            => door.HasCustomPermissions(out _);

        public static void SetCustomPermissions(this DoorVariant door, KeycardPermissions keycardPermissions)
            => _customPerms[door.netId] = keycardPermissions;

        public static void Override(this DoorVariant door, Func<DoorVariant, ReferenceHub, bool> modifier)
            => _customAccessModifiers[door.netId] = modifier;

        public static void ClearCustomPermissions(this DoorVariant door)
            => _customPerms.Remove(door.netId);

        public static void DisableInteracting(this DoorVariant door)
            => _disabled.Add(door.netId);

        public static void EnableInteracting(this DoorVariant door)
            => _disabled.Remove(door.netId);

        public static void ToggleInteracting(this DoorVariant door)
        {
            if (_disabled.Contains(door.netId))
                _disabled.Remove(door.netId);
            else
                _disabled.Add(door.netId);
        }

        public static void SetOpened(this DoorVariant door, bool isOpened = true)
            => door.NetworkTargetState = isOpened;

        public static bool Toggle(this DoorVariant door)
            => door.NetworkTargetState = !door.NetworkTargetState;

        public static void ToggleAfterDelay(this DoorVariant door, float seconds, out bool newState)
        {
            var state = !door.NetworkTargetState;
            newState = state;
            Calls.Delay(seconds, () => door.SetOpened(state));
        }

        public static void ToggleAfterDelay(this DoorVariant door, float seconds)
            => door.ToggleAfterDelay(seconds, out _);

        public static void Close(this DoorVariant door)
            => door.SetOpened(false);

        public static void CloseAfterDelay(this DoorVariant door, float seconds)
            => Calls.Delay(seconds, door.Close);

        public static void Open(this DoorVariant door)
            => door.SetOpened();

        public static void OpenAfterDelay(this DoorVariant door, float seconds)
            => Calls.Delay(seconds, door.Open);

        public static void Lock(this DoorVariant door, DoorLockReason lockType = DoorLockReason.AdminCommand)
            => door.ServerChangeLock(lockType, true);

        public static void LockAfterDelay(this DoorVariant door, float seconds, DoorLockReason lockType = DoorLockReason.AdminCommand)
            => Calls.Delay(seconds, () => door.Lock(lockType));

        public static void Unlock(this DoorVariant door, DoorLockReason lockType = DoorLockReason.AdminCommand)
            => door.ServerChangeLock(lockType, false);

        public static void UnlockAfterDelay(this DoorVariant door, float seconds, DoorLockReason lockType = DoorLockReason.AdminCommand)
            => Calls.Delay(seconds, () => door.Unlock(lockType));

        public static void UnlockAll(this DoorVariant door)
            => LockTypes.ForEach(door.Unlock);

        public static void UnlockAllAfterDelay(this DoorVariant door, float seconds)
            => Calls.Delay(seconds, door.UnlockAll);

        public static void Whitelist(this DoorVariant door, ReferenceHub hub)
        {
            if (_plyWhitelist.ContainsKey(door.netId))
                _plyWhitelist[door.netId].Add(hub.netId);
            else
                _plyWhitelist.Add(door.netId, new List<uint>() { hub.netId });
        }

        public static void Blacklist(this DoorVariant door, ReferenceHub hub)
        {
            if (_plyBlacklist.ContainsKey(door.netId))
                _plyBlacklist[door.netId].Add(hub.netId);
            else
                _plyBlacklist.Add(door.netId, new List<uint>() { hub.netId });
        }

        public static void Destroy(this DoorVariant door, bool clearDebris = false)
        {
            if (door is BreakableDoor breakableDoor)
                breakableDoor.Network_destroyed = true;

            if (clearDebris)
                Calls.Delay(1.25f, door.Delete);
        }

        public static void DestroyAfterDelay(this DoorVariant door, float seconds, bool clearDebris = false)
            => Calls.Delay(seconds, () => door.Destroy(clearDebris));

        public static float GetHealth(this DoorVariant door)
            => door is BreakableDoor breakableDoor ? breakableDoor.RemainingHealth : 0f;

        public static float GetMaxHealth(this DoorVariant door)
            => door is BreakableDoor breakableDoor ? breakableDoor.MaxHealth : 0f;

        public static void SetHealth(this DoorVariant door, float health)
        {
            if (door is BreakableDoor breakableDoor)
                breakableDoor.RemainingHealth = health;
        }

        public static void SetMaxHealth(this DoorVariant door, float max, bool healToFull = false)
        {
            if (door is BreakableDoor breakableDoor)
            {
                breakableDoor.MaxHealth = max;

                if (breakableDoor.RemainingHealth > breakableDoor.MaxHealth || healToFull)
                    breakableDoor.RemainingHealth = breakableDoor.MaxHealth;
            }
        }

        public static void Damage(this DoorVariant door, float damage, DoorDamageType damageType = DoorDamageType.ServerCommand)
        {
            if (door is BreakableDoor breakableDoor)
                breakableDoor.ServerDamage(damage, damageType);
        }

        public static void Delete(this DoorVariant door)
            => NetworkServer.UnSpawn(door.gameObject);

        public static void DeleteAfterDelay(this DoorVariant door, float seconds)
            => Calls.Delay(seconds, door.Delete);

        public static void PlayPermissionsDenied(this DoorVariant door, ReferenceHub hub)
            => door.PermissionsDenied(hub, 0);

        public static void DisableColliders(this DoorVariant door)
            => door._colliders.ForEach(collider => collider.isTrigger = false);

        public static void EnableColliders(this DoorVariant door)
            => door._colliders.ForEach(collider => collider.isTrigger = true);

        public static string GetTag(this DoorVariant door)
            => DoorNametagExtension.NamedDoors.TryGetFirst(d => d.Value.TargetDoor != null && d.Value.TargetDoor.netId == door.netId, out var target) ? target.Key : "Unnamed Door";

        public static void SetTag(this DoorVariant door, string newTag)
        {
            if (DoorNametagExtension.NamedDoors.TryGetFirst(d => d.Value.TargetDoor != null && d.Value.TargetDoor.netId == door.netId, out var target))
                target.Value.UpdateName(newTag);
            else
                door.gameObject.AddComponent<DoorNametagExtension>().UpdateName(newTag);
        }

        public static RoomIdentifier Room(this DoorVariant door)
            => RoomIdUtils.RoomAtPosition(door.transform.position);

        public static RoomName RoomId(this DoorVariant door)
            => door.Room()?.Name ?? RoomName.Unnamed;

        public static FacilityZone Zone(this DoorVariant door)
            => door.Room()?.Zone ?? FacilityZone.None;

        public static ReferenceHub[] PlayersInRadius(this DoorVariant door, float radius, FacilityZone[] zoneFilter = null, RoomName[] roomFilter = null)
            => Hub.InRadius(door.transform.position, radius, zoneFilter, roomFilter);

        [RoundStateChanged(RoundState.Restarting)]
        private static void OnRoundRestart()
        {
            _customAccessModifiers.Clear();
            _customPerms.Clear();
            _disabled.Clear();
            _plyWhitelist.Clear();
            _plyBlacklist.Clear();
        }

        [Patch(typeof(DoorVariant), nameof(DoorVariant.ServerInteract), PatchType.Prefix)]
        private static bool DoorInteractionPatch(DoorVariant __instance, ReferenceHub ply, byte colliderId)
        {
            if (__instance.HasCustomAccessModifier(out var modifier))
            {
                if (Calls.Delegate<bool>(modifier, __instance, ply, true))
                {
                    if (__instance.AllowInteracting(ply, colliderId))
                    {
                        __instance.Toggle();
                        __instance._triggerPlayer = ply;
                    }

                    return false;
                }
                else
                {
                    if (__instance.AllowInteracting(ply, colliderId))
                    {
                        __instance.PermissionsDenied(ply, colliderId);
                        DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);
                    }

                    return false;
                }
            }

            if (__instance.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
            {
                var type = DoorLockUtils.GetMode((DoorLockReason)__instance.ActiveLocks);

                if ((!type.HasFlagFast(DoorLockMode.CanClose) || !type.HasFlagFast(DoorLockMode.CanOpen)) 
                    && (!type.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP(true)) 
                    && (type == DoorLockMode.FullLock || (__instance.TargetState && !type.HasFlagFast(DoorLockMode.CanClose)) 
                    || (!__instance.TargetState && !type.HasFlagFast(DoorLockMode.CanOpen))))
                {
                    if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, false)))
                        return false;

                    __instance.LockBypassDenied(ply, colliderId);
                    return false;
                }
            }

            if (!__instance.AllowInteracting(ply, colliderId) || __instance.IsDisabled())
                return false;

            if (__instance.RequiresWhitelist() && !__instance.IsWhitelisted(ply))
                return false;

            if (__instance.IsBlacklisted(ply))
                return false;

            var canOpen = true;

            if (!__instance.HasCustomPermissions(out var perms))
                canOpen = ply.GetRoleId() is RoleTypeId.Scp079 || __instance.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);
            else
                canOpen = ply.GetRoleId() is RoleTypeId.Scp079 
                    || (perms is KeycardPermissions.None 
                    || (perms is KeycardPermissions.ScpOverride && ply.IsSCP(true)) 
                    || (ply.inventory.CurInstance != null && ply.inventory.CurInstance is KeycardItem keycard && keycard.Permissions.HasFlagFast(perms)));

            if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, __instance, canOpen)))
                return false;

            if (canOpen)
            {
                __instance.Toggle();
                __instance._triggerPlayer = ply;

                return false;
            }

            __instance.PermissionsDenied(ply, colliderId);
            DoorEvents.TriggerAction(__instance, DoorAction.AccessDenied, ply);

            return false;
        }
    }
}

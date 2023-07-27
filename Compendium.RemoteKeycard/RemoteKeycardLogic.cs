using Compendium.Features;

using helpers.Configuration.Ini;

using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using System.Linq;
using static PlayerList;
using UnityEngine;
using PluginAPI.Events;
using PlayerRoles;
using Compendium.Helpers;
using helpers.Extensions;
using Compendium.Helpers.Round;

namespace Compendium.RemoteKeycard
{
    public static class RemoteKeycardLogic
    {
        [IniConfig(Name = "Is Enabled", Description = "Whether or not to enable remote keycard.")]
        public static bool IsEnabled { get; set; } = true;

        [IniConfig(Name = "Affect Gates", Description = "Whether or not to affect gates.")]
        public static bool AffectGates { get; set; } = true;

        [IniConfig(Name = "Affect Doors", Description = "Whether or not to affect doors.")]
        public static bool AffectDoors { get; set; } = true;

        [IniConfig(Name = "Allow Shots", Description = "Whether or not to allow shots to open doors.")]
        public static bool AllowShots { get; set; } = true;

        [IniConfig(Name = "Targets", Description = "A list of targets affected by remote keycard. Door targets affect gates & doors, depending on your config settings.")]
        public static RemoteKeycardAccess[] AffectedTargets { get; set; } = new RemoteKeycardAccess[]
        {
            RemoteKeycardAccess.EntranceDoors,
            RemoteKeycardAccess.SurfaceDoors,

            RemoteKeycardAccess.HeavyContainmentDoors,
            RemoteKeycardAccess.LightContainmentDoors,

            RemoteKeycardAccess.Generators,

            RemoteKeycardAccess.GunLockers,
            RemoteKeycardAccess.WallGunLockers,
            RemoteKeycardAccess.Lockers,

            RemoteKeycardAccess.OutsiteWarheadPanel
        };

        public static bool CanBypass(RemoteKeycardAccess remoteKeycardAccess)
        {
            if (!IsEnabled)
                return false;

            if (remoteKeycardAccess.IsDoor())
            {
                if (!AffectDoors && !AffectGates)
                {
                    return false;
                }
            }

            if (AffectedTargets is null)
            {
                FLog.Warn($"The Affected Targets config array is null!");
                return true;
            }

            return AffectedTargets.Contains(remoteKeycardAccess);
        }

        public static bool CanBypass(ReferenceHub hub, DoorVariant door)
        {
            if (!CanBypass(door.GetDoorCategory()))
                return false;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (!(item is KeycardItem keycard))
                    continue;

                if (door.RequiredPermissions.CheckPermissions(keycard, hub))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CanBypass(ReferenceHub hub, Scp079Generator generator)
        {
            if (!CanBypass(RemoteKeycardAccess.Generators))
                return false;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (item is KeycardItem keycard)
                {
                    if (keycard.Permissions.HasFlagFast(generator._requiredPermission))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool CanBypass(ReferenceHub hub, LockerChamber locker)
        {
            if (!CanBypass(locker.GetLockerCategory()))
                return false;

            if (locker.RequiredPermissions is KeycardPermissions.None)
                return true;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (item is KeycardItem keycard)
                {
                    if (keycard.Permissions.HasFlagFast(locker.RequiredPermissions))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool CanBypass(ReferenceHub hub)
        {
            if (!CanBypass(RemoteKeycardAccess.OutsiteWarheadPanel))
                return false;

            foreach (var item in hub.inventory.UserInventory.Items.Values)
            {
                if (item is KeycardItem keycard)
                {
                    if (keycard.Permissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static RemoteKeycardAccess GetDoorCategory(this DoorVariant door)
        {
            var room = door.Rooms.First();

            if (room.Zone is MapGeneration.FacilityZone.Entrance)
                return RemoteKeycardAccess.EntranceDoors;
            else if (room.Zone is MapGeneration.FacilityZone.HeavyContainment)
                return RemoteKeycardAccess.HeavyContainmentDoors;
            else if (room.Zone is MapGeneration.FacilityZone.LightContainment)
                return RemoteKeycardAccess.LightContainmentDoors;
            else
                return RemoteKeycardAccess.SurfaceDoors;
        }

        public static RemoteKeycardAccess GetLockerCategory(this LockerChamber locker)
        {
            if (locker.name.Contains("LargeGunLockerStructure"))
                return RemoteKeycardAccess.GunLockers;
            else if (locker.name.Contains("MiscLocker"))
                return RemoteKeycardAccess.Lockers;
            else
                return RemoteKeycardAccess.WallGunLockers;
        }

        private static bool IsDoor(this RemoteKeycardAccess remoteKeycardAccess) => 
               remoteKeycardAccess is RemoteKeycardAccess.EntranceDoors 
            || remoteKeycardAccess is RemoteKeycardAccess.HeavyContainmentDoors 
            || remoteKeycardAccess is RemoteKeycardAccess.LightContainmentDoors 
            || remoteKeycardAccess is RemoteKeycardAccess.SurfaceDoors;

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStarted()
        {
            DoorVariant.AllDoors.ForEach(door =>
            {
                door.Override(CanInteract);
            });

            FLog.Debug($"Overriden all door logic.");
        }

        private static bool CanInteract(DoorVariant target, ReferenceHub ply)
        {
            if (target.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
            {
                var mode = DoorLockUtils.GetMode((DoorLockReason)target.ActiveLocks);
                if ((!mode.HasFlagFast(DoorLockMode.CanClose) || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride) || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock || (target.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose)) || (!target.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
                {
                    if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, target, false)))
                        return false;

                    target.LockBypassDenied(ply, 0);
                    return false;
                }
            }

            if (!target.AllowInteracting(ply, 0))
                return false;

            var flag = ply.GetRoleId() == RoleTypeId.Scp079 || target.RequiredPermissions.CheckPermissions(ply.inventory.CurInstance, ply);

            if (!flag)
            {
                if (CanBypass(ply, target))
                    flag = true;
            }

            if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, target, flag)))
                return false;

            return flag;
        }
    }
}
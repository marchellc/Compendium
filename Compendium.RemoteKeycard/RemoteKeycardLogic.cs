using helpers.Configuration.Ini;

using Interactables.Interobjects.DoorUtils;

using InventorySystem.Items.Keycards;

using MapGeneration.Distributors;

using System.Linq;

namespace Compendium.RemoteKeycard
{
    public static class RemoteKeycardLogic
    {
        [IniConfig("IsEnabled", null, "Whether or not to enable remote keycard.")]
        public static bool IsEnabled { get; set; } = true;

        [IniConfig("AffectGates", null, "Whether or not to affect gates.")]
        public static bool AffectGates { get; set; } = true;

        [IniConfig("AffectDoors", null, "Whether or not to affect doors.")]
        public static bool AffectDoors { get; set; } = true;

        [IniConfig("Targets", null, "A list of targets affected by remote keycard. Door targets affect gates & doors, depending on your config settings.")]
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
                Plugin.Warn($"The Affected Targets config array is null!");
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
    }
}
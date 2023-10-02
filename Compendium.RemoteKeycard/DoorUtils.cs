using Compendium.RemoteKeycard.Enums;

using Interactables.Interobjects.DoorUtils;

using MapGeneration.Distributors;

using System.Linq;

namespace Compendium.RemoteKeycard
{
    public static class DoorUtils
    {
        public static bool IsDoor(this InteractableCategory category)
            => category is InteractableCategory.EzDoor || category is InteractableCategory.HczDoor || category is InteractableCategory.LczDoor
            || category is InteractableCategory.SurfaceDoor;

        public static bool IsLocker(this InteractableCategory category)
            => category is InteractableCategory.WallGunLocker || category is InteractableCategory.Locker || category is InteractableCategory.GunLocker;

        public static InteractableCategory GetCategory(this DoorVariant door)
        {
            var room = door.Rooms.First();

            if (room.Zone is MapGeneration.FacilityZone.Entrance)
                return InteractableCategory.EzDoor;
            else if (room.Zone is MapGeneration.FacilityZone.HeavyContainment)
                return InteractableCategory.HczDoor;
            else if (room.Zone is MapGeneration.FacilityZone.LightContainment)
                return InteractableCategory.LczDoor;
            else
                return InteractableCategory.SurfaceDoor;
        }
    
        public static InteractableCategory GetCategory(this LockerChamber locker)
        {
            var obj = locker.transform.parent.gameObject;

            if (obj.name.Contains("LargeGunLockerStructure"))
                return InteractableCategory.GunLocker;
            else if (locker.name.Contains("MiscLocker"))
                return InteractableCategory.Locker;
            else
                return InteractableCategory.WallGunLocker;
        }
    }
}
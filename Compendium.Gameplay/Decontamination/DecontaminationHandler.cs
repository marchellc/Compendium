using helpers.Configuration;
using helpers.Patching;

using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

using LightContainmentZoneDecontamination;

namespace Compendium.Gameplay.Decontamination
{
    public static class DecontaminationHandler
    {
        [Config(Name = "Lift Lockdown Delay", Description = "The amount of milliseconds to wait before locking all Light Containment Zone elevators.")]
        public static int LiftLockdownDelay { get; set; } = 0;

        [Config(Name = "Lift Send Down", Description = "Whether or not to send elevators down to the LCZ zone once decontamination starts.")]
        public static bool LiftSendDown { get; set; } = true;

        [Patch(typeof(DecontaminationController), nameof(DecontaminationController.DisableElevators), PatchType.Prefix)]
        private static bool DisableElevatorsPatch(DecontaminationController __instance)
        {
            if (LiftLockdownDelay <= 0 && LiftSendDown)
                return true;

            if (LiftLockdownDelay > 0)
                Calls.Delay(LiftLockdownDelay, () => DoLiftLockdown(__instance));
            else
                DoLiftLockdown(__instance);

            return false;
        }

        private static void DoLiftLockdown(DecontaminationController __instance)
        {
            var flag = false;

            foreach (var door in DoorVariant.AllDoors)
            {
                if (door is ElevatorDoor liftDoor)
                {
                    if (liftDoor.Rooms.Length != 0 && liftDoor.Rooms[0].Zone is MapGeneration.FacilityZone.LightContainment)
                    {
                        liftDoor.Lock(DoorLockReason.DecontLockdown);

                        if (!door.TargetState && LiftSendDown && !ElevatorManager.TrySetDestination(liftDoor.Group, 1))
                            flag = true;
                    }
                }
            }

            if (flag)
                return;

            __instance._elevatorsDirty = false;
        }
    }
}
using Compendium.Extensions;
using Compendium;
using Compendium.Calls;

using helpers.Extensions;
using helpers.Random;

using Interactables.Interobjects.DoorUtils;

using PlayerRoles;

using System.Linq;

using UnityEngine;

namespace Compendium.BetterTesla
{
    public class TeslaDamageStatus
    {
        private TeslaGate m_Tesla;
        private float m_RemainingHealth;

        public TeslaDamageStatus(TeslaGate teslaGate)
        {
            m_Tesla = teslaGate;
            m_RemainingHealth = BetterTeslaLogic.TeslaHealth;
        }

        public bool IsDisabled()
            => m_RemainingHealth <= 0f;

        public void ProcessDamage(float damage, bool isGrenade = false)
        {
            if (IsDisabled())
                return;

            m_RemainingHealth -= damage;

            if (IsDisabled())
            {
                var time = Mathf.CeilToInt(Random.Range(BetterTeslaLogic.MinTeslaTimeout, BetterTeslaLogic.MaxTeslaTimeout));

                if (isGrenade && BetterTeslaLogic.GrenadeTimeMultiplier != -1)
                    time *= BetterTeslaLogic.GrenadeTimeMultiplier;

                if (BetterTeslaLogic.DamagedTeslaHint)
                {
                    foreach (var hub in ReferenceHub.AllHubs)
                    {
                        if (hub.Mode != ClientInstanceMode.ReadyClient)
                            continue;

                        if (!hub.IsAlive())
                            continue;

                        if (!hub.IsWithinDistance(m_Tesla.transform.position, BetterTeslaLogic.DamagedTeslaRadius))
                            continue;

                        hub.Hint($"\n\n<b><color=#33FFA5>Tesla Gate <color=#FF0000>disabled</color> for <color=#FF0000>{time}</color> second(s)</color></b>!", 3f, true);
                    }
                }

                if (BetterTeslaLogic.DamagedBlackout && BetterTeslaLogic.DamagedBlackoutDuration > 0f)
                {
                    if (m_Tesla.Room != null)
                    {
                        var doors = DoorVariant.AllDoors.Where(door => door.Rooms.Contains(m_Tesla.Room));
                        var lights = RoomLightController.Instances.Where(light => light != null && light.Room == m_Tesla.Room);

                        doors.ForEach(door =>
                        {
                            door.NetworkTargetState = WeightedRandomGeneration.Default.GetBool(30);
                            door.ServerChangeLock(DoorLockReason.AdminCommand, true);
                        });

                        lights.ForEach(light =>
                        {
                            light.ServerFlickerLights(BetterTeslaLogic.DamagedBlackoutDuration);
                        });

                        CallHelper.CallWithDelay(() =>
                        {
                            doors.ForEach(door =>
                            {
                                door.ServerChangeLock(DoorLockReason.AdminCommand, false);
                            });
                        }, BetterTeslaLogic.DamagedBlackoutDuration + 0.2f);
                    }
                }

                CallHelper.CallWithDelay(() =>
                {
                    Reset();
                }, time);
            }
        }

        private void Reset()
        {
            m_RemainingHealth = BetterTeslaLogic.TeslaHealth;
        }
    }
}
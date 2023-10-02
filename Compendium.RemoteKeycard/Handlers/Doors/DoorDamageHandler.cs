using Compendium.Events;
using Compendium.RemoteKeycard.Enums;
using Compendium.Round;

using helpers;
using helpers.Configuration;
using helpers.Random;

using Interactables.Interobjects.DoorUtils;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace Compendium.RemoteKeycard.Handlers.Doors
{
    [ConfigCategory(Name = "Door Damage")]
    public static class DoorDamageHandler
    {
        private static readonly Dictionary<DoorVariant, DoorDamageData> _damage = new Dictionary<DoorVariant, DoorDamageData>();
        private static readonly Dictionary<DoorVariant, DoorZombieStatus> _zombies = new Dictionary<DoorVariant, DoorZombieStatus>();

        [Config(Name = "Enabled", Description = "Whether or not to allow players to damage doors.")]
        public static bool IsEnabled { get; set; } = true;

        [Config(Name = "Health", Description = "Health of each door type.")]
        public static Dictionary<InteractableCategory, float> DoorHealth { get; set; } = new Dictionary<InteractableCategory, float>()
        {
            [InteractableCategory.EzDoor] = 100f,
            [InteractableCategory.EzGate] = 300f,

            [InteractableCategory.SurfaceDoor] = 100f,
            [InteractableCategory.SurfaceGate] = 300f,

            [InteractableCategory.LczDoor] = 150f,
            [InteractableCategory.LczGate] = 300f,

            [InteractableCategory.HczDoor] = 200f,
            [InteractableCategory.HczGate] = 300f
        };

        [Config(Name = "Destroy Status", Description = "The door status to use for a destroyed door.")]
        public static DoorDamageStatus DestroyStatus { get; set; } = DoorDamageStatus.Unusable;

        [Config(Name = "Zombies", Description = "Config for SCP-049-2 attackers.")]
        public static DoorZombieConfig Zombies { get; set; } = new DoorZombieConfig();

        public static void DoDamage(ReferenceHub player, float damage, DoorVariant target, DoorDamageSource source)
        {
            if (!IsEnabled)
                return;

            if (source is DoorDamageSource.Firearm)
            {
                if (!_damage.TryGetValue(target, out var data))
                    return;

                if (data.Status != DoorDamageStatus.Usable)
                    return;

                data.Health -= damage;

                if (data.Health <= 0f)
                {
                    data.Status = DestroyStatus;
                    target.Open();
                }
            }
            else
            {
                if (!_zombies.TryGetValue(target, out var zombieStatus) || zombieStatus.Broken)
                    return;

                if (player.RoleId() != PlayerRoles.RoleTypeId.Scp0492)
                    return;

                if (target.NetworkTargetState)
                    return;

                var lockType = DoorLockUtils.GetMode((DoorLockReason)target.ActiveLocks);

                if (lockType.HasFlagFast(DoorLockMode.FullLock) 
                    && !lockType.HasFlagFast(DoorLockMode.ScpOverride) 
                    && !lockType.HasFlagFast(DoorLockMode.CanOpen))
                    return;

                if (target.RequiredPermissions.RequiredPermissions == KeycardPermissions.None)
                    return;

                if (!zombieStatus.CurrentInteractions.Contains(player))
                    zombieStatus.CurrentInteractions.Add(player);

                zombieStatus.LastInteraction = DateTime.Now;
                zombieStatus.ActiveInteraction = true;
                zombieStatus.RemainingHealth -= zombieStatus.Damage;

                if (zombieStatus.RemainingHealth <= 0f)
                {
                    zombieStatus.RemainingHealth = 0f;
                    zombieStatus.ActiveInteraction = false;
                    zombieStatus.Broken = true;
                    zombieStatus.CurrentInteractions.Clear();
                    zombieStatus.LastInteraction = DateTime.Now;

                    target.Destroy();
                }
                else
                {
                    if (Zombies.InteractionHint != null && Zombies.InteractionHint.IsValid())
                        player.Broadcast(Zombies.InteractionHint.Message.Replace("%hp%", Mathf.RoundToInt(zombieStatus.RemainingHealth).ToString())
                                                                        .Replace("%damage%", Mathf.RoundToInt(zombieStatus.Damage).ToString()),
                                    (int)Zombies.InteractionHint.Duration);
                }
            }
        }

        public static bool ProcessDamage(ReferenceHub player, DoorVariant door)
        {
            if (!IsEnabled)
                return true;

            if (!_damage.TryGetValue(door, out var data))
                return true;

            if (data.Status != DoorDamageStatus.Usable)
            {
                if (data.Status is DoorDamageStatus.Unusable)
                    return false;

                if (data.Status is DoorDamageStatus.UsableChance)
                {
                    if (DoorHandler.UsableChance <= 0)
                        return false;

                    return WeightedRandomGeneration.Default.GetBool(DoorHandler.UsableChance);
                }
            }

            return true;
        }

        public static void DamageAction(ReferenceHub player, DoorVariant door)
        {
            if (!IsEnabled)
                return;

            if (DoorHandler.FailureHint != null && DoorHandler.FailureHint.IsValid())
                DoorHandler.FailureHint.Send(player);
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStart()
        {
            _damage.Clear();
            _zombies.Clear();

            DoorVariant.AllDoors.ForEach(d =>
            {
                var category = d.GetCategory();

                if (DoorHealth.TryGetValue(category, out var health))
                    _damage[d] = new DoorDamageData()
                    {
                        Health = health,
                        MaxHealth = health,

                        Status = DoorDamageStatus.Usable
                    };

                _zombies[d] = new DoorZombieStatus
                {
                    ActiveInteraction = false,
                    Broken = false,
                    
                    DamagePerPlayer = Zombies.DamagePerPlayer,
                    RegenHealth = Zombies.RegenHealth,
                    RegenSpeed = Zombies.RegenSpeed,

                    LastInteraction = DateTime.Now,
                    LastRegen = DateTime.Now
                };

                if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelOne))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 15f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelTwo))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 30f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ArmoryLevelThree))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 60f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.AlphaWarhead))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 100f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.Intercom))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 10f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelOne))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 5f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelTwo))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 30f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ContainmentLevelThree))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 100f;
                else if (d.RequiredPermissions.RequiredPermissions.HasFlagFast(KeycardPermissions.ExitGates))
                    _zombies[d].StartingHealth = Zombies.StartingHealth + 2000f;
                else
                    _zombies[d].StartingHealth = Zombies.StartingHealth;

                _zombies[d].RemainingHealth = _zombies[d].StartingHealth;
            });
        }

        [UpdateEvent(IsMainThread = true, TickRate = 100, Type = Update.UpdateHandlerType.Engine)]
        private static void UpdateZombieProgress()
        {
            _zombies.PoolableModifyAct((door, dict) =>
            {
                if (!dict.TryGetValue(door, out var status))
                    return;

                if (status.ActiveInteraction
                    && (DateTime.Now - status.LastInteraction).TotalMilliseconds >= Zombies.LastInteractionInterval)
                {
                    status.CurrentInteractions.Clear();
                    status.ActiveInteraction = false;

                    return;
                }

                if (status.RemainingHealth > 0f)
                {
                    if (status.RemainingHealth < status.StartingHealth)
                    {
                        if ((DateTime.Now - status.LastRegen).TotalMilliseconds >= Zombies.RegenSpeed)
                        {
                            status.RemainingHealth += status.RegenHealth;

                            if (status.RemainingHealth > status.StartingHealth)
                                status.RemainingHealth = status.StartingHealth;

                            status.LastRegen = DateTime.Now;
                        }
                    }
                }
            });
        }
    }
}
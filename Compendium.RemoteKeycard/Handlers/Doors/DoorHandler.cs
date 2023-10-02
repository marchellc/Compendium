using Interactables.Interobjects.DoorUtils;
using Interactables.Interobjects;

using System;

using UnityEngine;

using PluginAPI.Events;

using Compendium.Round;
using Compendium.Extensions;
using Compendium.Hints;
using Compendium.Colors;

using helpers;
using helpers.Configuration;
using helpers.Random;
using helpers.Attributes;
using helpers.Patching;

using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps;

using Mirror;

using LightContainmentZoneDecontamination;

namespace Compendium.RemoteKeycard.Handlers.Doors
{
    [ConfigCategory(Name = "Door")]
    public static class DoorHandler
    {
        public const int Mask = ~(1 << 13 | 1 << 16);

        public static event Func<ReferenceHub, GameObject, bool> OnRaycast;
        public static event Func<ReferenceHub, bool> OnZombieAttack;

        [Config(Name = "Enabled", Description = "Whether or not to enable remote keycard interactions for doors.")]
        public static bool IsEnabled { get; set; } = true;

        [Config(Name = "Shot", Description = "Whether or not to allow opening doors by shooting them.")]
        public static bool AllowShot { get; set; } = true;

        [Config(Name = "Failure Chance", Description = "The chance for a remote keycard interaction to fail.")]
        public static int FailureChance { get; set; } = 5;

        [Config(Name = "Usable Chance", Description = "The chance for a door to work after being destroyed.")]
        public static int UsableChance { get; set; } = 60;

        [Config(Name = "Base Damage", Description = "Base damage that firearm deal to doors.")]
        public static float BaseDamage { get; set; } = 50f;

        [Config(Name = "Failure Hint", Description = "The hint to display when a door interaction fails.")]
        public static HintInfo FailureHint { get; set; } = HintInfo.Get(
            $"\n\n\n" +
            $"<b><color={ColorValues.LightGreen}>Z nějakého důvodu tyto dveře <color={ColorValues.Red}>nefungují</color> ..</color></b>", 5f);

        [Load]
        private static void Load()
        {
            ShootHandler.OnHit += OnHit;
            OnZombieAttack += OnZombieAttackHandler;
        }

        [Unload]
        private static void Unload()
        {
            ShootHandler.OnHit -= OnHit;
            OnZombieAttack -= OnZombieAttackHandler;
        }

        private static bool CanInteractOverride(DoorVariant target, ReferenceHub ply)
        {
            if (!RoundSwitches.IsDoorDisabled)
            {
                if (!DoorDamageHandler.ProcessDamage(ply, target)
                    && !ply.serverRoles.BypassMode)
                {
                    DoorDamageHandler.DamageAction(ply, target);
                    return false;
                }

                if (FailureChance > 0 && WeightedRandomGeneration.Default.GetBool(FailureChance))
                {
                    if (FailureHint != null && FailureHint.IsValid())
                        FailureHint.Send(ply);

                    return false;
                }
            }

            if (target.ActiveLocks > 0 && !ply.serverRoles.BypassMode)
            {
                var mode = DoorLockUtils.GetMode((DoorLockReason)target.ActiveLocks);

                if ((!mode.HasFlagFast(DoorLockMode.CanClose)
                    || !mode.HasFlagFast(DoorLockMode.CanOpen)) && (!mode.HasFlagFast(DoorLockMode.ScpOverride)
                    || !ply.IsSCP(true)) && (mode == DoorLockMode.FullLock || (target.TargetState && !mode.HasFlagFast(DoorLockMode.CanClose))
                    || (!target.TargetState && !mode.HasFlagFast(DoorLockMode.CanOpen))))
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

            if (!RoundSwitches.IsDoorDisabled)
            {
                if (!flag)
                    flag = AccessUtils.CanAccessDoor(target, ply);
            }

            if (!EventManager.ExecuteEvent(new PlayerInteractDoorEvent(ply, target, flag)))
                return false;

            return flag;
        }

        [RoundStateChanged(RoundState.InProgress)]
        private static void OnRoundStart()
        {
            if (!IsEnabled)
                return;

            DoorVariant.AllDoors.ForEach(d =>
            {
                d.Override(CanInteractOverride);
            });
        }

        private static bool OnZombieAttackHandler(ReferenceHub ply)
        {
            if (!Physics.Raycast(ply.PlayerCameraReference.position, ply.PlayerCameraReference.forward, out var rayHit, 20f, Mask))
                return true;

            var obj = rayHit.transform?.parent?.gameObject ?? rayHit.transform.gameObject;

            if (obj is null)
                return true;

            if (obj.TryGet<DoorVariant>(out var door) || obj.TryGet<RegularDoorButton>(out var button) && (door = button.Target as DoorVariant) != null)
                DoorDamageHandler.DoDamage(ply, 0f, door, DoorDamageSource.Zombie);

            return true;
        }

        private static bool OnHit(ReferenceHub ply, Ray ray, RaycastHit hit, GameObject target)
        {
            if (!Physics.Raycast(ply.PlayerCameraReference.position, ply.PlayerCameraReference.forward, out var rayHit, 70f, Mask)
                || rayHit.collider is null)
                return true;

            var obj = rayHit.transform?.parent?.gameObject ?? rayHit.transform.gameObject;

            if (obj is null)
                return true;

            if (IsEnabled && AllowShot)
            {
                if (obj.TryGet<RegularDoorButton>(out var button))
                {
                    var door = button.GetComponentInParent<DoorVariant>();

                    if (door != null)
                    {
                        if (BaseDamage > 0f)
                            DoorDamageHandler.DoDamage(ply, (BaseDamage / ply.DistanceSquared(door)) * 10f, door, DoorDamageSource.Firearm);

                        door.ServerInteract(ply, 0);
                    }
                }
                else if (obj.TryGet<ElevatorPanel>(out var panel))
                {
                    if (panel.AssignedChamber != null &&
                        panel.AssignedChamber.IsReady

                        && ElevatorDoor.AllElevatorDoors.TryGetValue(panel.AssignedChamber.AssignedGroup, out var list)
                        && DoorLockUtils.GetMode(panel.AssignedChamber.ActiveLocks) != DoorLockMode.FullLock)
                    {
                        if (DecontaminationController.Singleton != null && DecontaminationController.Singleton._decontaminationBegun)
                            return true;

                        if (AlphaWarheadController.Detonated)
                            return true;

                        var nextlevel = panel.AssignedChamber.CurrentLevel + 1;

                        if (nextlevel >= list.Count)
                            nextlevel = 0;

                        panel.AssignedChamber.TrySetDestination(nextlevel);

                        NetworkServer.SendToReady(new ElevatorManager.ElevatorSyncMsg(panel.AssignedChamber.AssignedGroup, panel.AssignedChamber.CurrentLevel));
                        ElevatorManager.SyncedDestinations[panel.AssignedChamber.AssignedGroup] = panel.AssignedChamber.CurrentLevel;
                    }
                }
            }

            if (OnRaycast != null)
                return OnRaycast(ply, obj);

            return true;
        }

        [Patch(typeof(ScpAttackAbilityBase<ZombieRole>), nameof(ScpAttackAbilityBase<ZombieRole>.ServerProcessCmd), PatchType.Prefix)]
        private static bool OnAttack(ZombieAttackAbility __instance, NetworkReader reader)
        {
            if (OnZombieAttack != null && !OnZombieAttack(__instance.Owner))
                return false;

            var num = Physics.OverlapSphereNonAlloc(
                __instance.OverlapSphereOrigin, 
                __instance._detectionRadius, 
                
                ScpAttackAbilityBase<ZombieRole>.DetectionsNonAlloc, 
                ScpAttackAbilityBase<ZombieRole>.DetectionMask);
            
            __instance._syncAttack = AttackResult.None;

            for (int i = 0; i < num; i++)
            {
                if (ScpAttackAbilityBase<ZombieRole>.DetectionsNonAlloc[i].TryGetComponent<IDestructible>(out var destructible)
                    && destructible.NetworkId != __instance.Owner.netId)
                {
                    var hitbox = destructible as HitboxIdentity;

                    if (hitbox is null)
                    {
                        __instance.DamageDestructible(destructible);
                        __instance._syncAttack |= AttackResult.AttackedObject;
                    }
                    else if (hitbox.TargetHub.roleManager.CurrentRole is HumanRole)
                        __instance.DetectedPlayers.Add(hitbox.TargetHub);
                }

            }

            __instance.DamagePlayers();
            __instance.ServerSendRpc(true);

            return false;
        }
    }
}

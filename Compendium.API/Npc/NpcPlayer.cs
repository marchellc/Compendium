using Compendium.Npc.Targeting;

using helpers;
using helpers.Pooling;
using helpers.Random;

using Mirror;

using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Compendium.Npc
{
    public class NpcPlayer : Poolable
    {
        private bool PropsSet;
        private bool MovReg;

        public virtual ReferenceHub Hub { get; private set; }
        public virtual Scp079Camera Camera { get; set; }
        public virtual NetworkConnectionToClient Connection { get; private set; }

        public virtual NpcTarget Target { get; set; }

        public virtual Vector3 Position => Hub.Position();
        public virtual Quaternion Rotation => Hub.Rotation();

        public virtual RoleTypeId RoleId { get => Hub.GetRoleId(); set => Hub.roleManager.ServerSetRole(value, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None); }
        public virtual ItemType HeldItem
        {
            get => Hub.inventory.NetworkCurItem.TypeId;
            set
            {
                if (!Hub.inventory.UserInventory.Items.Any(p => p.Value.ItemTypeId == value))
                {
                    Hub.ClearItems();
                    Hub.inventory.NetworkCurItem = new InventorySystem.Items.ItemIdentifier(value, Hub.AddItem(value, false).ItemSerial);
                }
                else
                {
                    var item = Hub.inventory.UserInventory.Items.First(p => p.Value.ItemTypeId == value);
                    Hub.inventory.NetworkCurItem = new InventorySystem.Items.ItemIdentifier(item.Value.ItemTypeId, item.Key);
                }
            }
        }

        public virtual PlayerRoleBase Role { get => Hub.roleManager.CurrentRole; set => Hub.roleManager.CurrentRole = value; }

        public virtual NpcMovementMode? ForcedMode { get; private set; }

        public virtual Dictionary<NpcMovementMode, float> Distancing { get; } = new Dictionary<NpcMovementMode, float>()
        {
            [NpcMovementMode.Running] = 8f,
            [NpcMovementMode.Walking] = 5f,
            [NpcMovementMode.Teleport] = 10f
        };

        public virtual Dictionary<NpcMovementMode, float> Speed { get; } = new Dictionary<NpcMovementMode, float>()
        {
            [NpcMovementMode.Running] = 10f,
            [NpcMovementMode.Walking] = 5f,
            [NpcMovementMode.Teleport] = 0f
        };

        public virtual bool IsSpawned { get; private set; }

        public virtual bool Enable079Logic { get; set; }

        public virtual bool ShowInSpectatorList { get; set; }
        public virtual bool ShowInPlayerList { get; set; }

        public virtual string Nick { get => Hub.nicknameSync.MyNick; set => Hub.nicknameSync.MyNick = value; }
        public virtual string UserId { get => Hub.characterClassManager._privUserId; set => Hub.characterClassManager._privUserId = value; }
        public virtual string CustomId { get; set; }

        public virtual int Id { get => Hub._playerId.Value; set => Hub.Network_playerId = new RecyclablePlayerId(value); }

        public virtual float CurrentSpeed
        {
            get
            {
                if (ForcedSpeed.HasValue)
                    return ForcedSpeed.Value;

                var distance = Vector3.Distance(Target.Position, Position);

                if (distance >= Speed[NpcMovementMode.Running])
                    return 0f;

                if (distance >= Speed[NpcMovementMode.Walking])
                    return Speed[NpcMovementMode.Running];

                return Speed[NpcMovementMode.Walking];
            }
        }

        public virtual NpcMovementMode MovementMode
        {
            get
            {
                var distance = Vector3.Distance(Target.Position, Position);

                if (distance >= Distancing[NpcMovementMode.Running])
                    return NpcMovementMode.Teleport;

                if (distance >= Distancing[NpcMovementMode.Walking])
                    return NpcMovementMode.Running;

                return NpcMovementMode.Walking;
            }
        }

        public virtual float? ForcedSpeed { get; set; }

        public NpcPlayer()
        {
            NpcManager.OnNpcCreated(this);
        }

        public virtual void Despawn()
        {
            if (!IsSpawned) 
                return;

            IsSpawned = false;

            Target = null;
            ForcedMode = null;
            ForcedSpeed = null;
            Camera = null;

            Hub.MakeInvisible();

            NpcManager.OnNpcDespawned(this);

            Plugin.Info($"NPC despawned '{CustomId}'");
        }

        public virtual void Destroy()
        {
            Hub.OnDestroy();

            NpcManager.NpcHubs.Remove(Hub);

            CustomNetworkManager.TypedSingleton?.OnServerDisconnect(Hub.connectionToClient);

            UnityEngine.Object.Destroy(Hub.gameObject);

            Target = null;
            ForcedMode = null;
            ForcedSpeed = null;
            Hub = null;
            Connection = null;
            PropsSet = false;

            Plugin.Info($"NPC destroyed '{CustomId}'");

            CustomId = null;

            NpcManager.OnNpcDestroyed(this);
        }

        public virtual void Move(Vector3 destination)
            => Target = new PositionTarget(destination);

        public virtual void Spawn(Action<NpcPlayer> modify = null)
        {
            try
            {
                if (Hub is null)
                {
                    Hub = NpcManager.NewHub;
                    PropsSet = false;
                }

                if (Hub is null)
                {
                    PluginAPI.Core.Log.Error($"Failed to instantiate a new ReferenceHub prefab!", "Npc Base");
                    PropsSet = false;
                    return;
                }

                if (!MovReg)
                    MovReg = Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", MovementLogic);

                modify?.Invoke(this);

                if (!PropsSet)
                {
                    NetworkServer.AddPlayerForConnection(Connection = new NpcConnection(Id), Hub.gameObject);

                    Hub.characterClassManager._privUserId = "npc@server";
                    Hub.characterClassManager._targetInstanceMode = ClientInstanceMode.DedicatedServer;

                    try
                    {
                        Hub.nicknameSync.MyNick = $"NPC [{Id}]";
                    }
                    catch { }

                    CustomId = RandomGeneration.Default.GetRandom(1, 100).ToString();

                    PropsSet = true;
                    IsSpawned = true;

                    NpcManager.OnNpcSpawned(this);

                    Calls.Delay(0.2f, () =>
                    {
                        Hub.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                    });

                    Plugin.Info($"NPC created '{CustomId}'");
                }

                if (!IsSpawned)
                {
                    Hub.MakeVisible();

                    NpcManager.OnNpcSpawned(this);

                    Plugin.Info($"NPC spawned '{CustomId}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }

        public virtual void Teleport(Vector3 location)
        {
            if (!IsSpawned)
                return;

            Hub.TryOverridePosition(location, Vector3.zero);
        }

        private void MovementLogic()
        {
            if (Hub is null || !Hub.IsAlive() || IsPooled || !IsSpawned)
                return;

            var hasTarget = Target != null && Target.IsValid;

            if (RoleId is RoleTypeId.Scp079)
            {
                if (Enable079Logic)
                    UpdateScp079(hasTarget);

                return;
            }

            UpdateMovement(hasTarget);
            UpdateCamera(hasTarget);
        }

        public virtual void UpdateCamera(bool hasTarget)
        {
            if (!hasTarget || IsPooled || !IsSpawned)
                return;

            if (!(Hub.Role() is IFpcRole fpcRole))
                return;

            var mouseLook = fpcRole.FpcModule.MouseLook;
            var eulerAngles = Quaternion.LookRotation(Target.Position - Position, Vector3.up).eulerAngles;

            mouseLook.CurrentHorizontal = eulerAngles.y;
            mouseLook.CurrentVertical = eulerAngles.x;
        }

        public virtual void UpdateMovement(bool hasTarget)
        {
            if (!hasTarget || IsPooled || !IsSpawned)
                return;

            if (!(Hub.Role() is IFpcRole fpcRole))
                return;

            var direction = (Target.Position - Position).normalized;
            var velocity = direction * CurrentSpeed;

            fpcRole.FpcModule.CurrentMovementState = NpcHelper.TranslateMode(MovementMode);
            fpcRole.FpcModule.CharController.Move(velocity * Time.deltaTime);
        }

        public virtual void UpdateScp079(bool hasTarget)
        {
            if (!hasTarget || IsPooled || !IsSpawned)
                return;

            var scp = Hub.roleManager.CurrentRole as Scp079Role;

            if (scp is null)
                return;

            var closestCamera = NpcHelper.GetClosestCamera(Target.Position);

            if (closestCamera is null)
                return;

            if (Camera != null && (closestCamera.Room == Camera.Room))
                return;

            Camera = closestCamera;

            scp._curCamSync._lastCam = Camera;
            scp._curCamSync.ClientSwitchTo(closestCamera);

            var rotation = Quaternion.LookRotation(Target.Position - Position, Vector3.up).eulerAngles;

            Camera.HorizontalRotation = rotation.x;
            Camera.VerticalRotation = rotation.y;
            Camera.RollRotation = rotation.z;
        }

        public override void OnPooled()
        {
            base.OnPooled();
            Despawn();
            Plugin.Info($"NPC pooled '{CustomId}'");
        }

        public override void OnUnpooled()
        {
            base.OnUnpooled();
            Spawn();
            Plugin.Info($"NPC unpooled '{CustomId}'");
        }
    }
}
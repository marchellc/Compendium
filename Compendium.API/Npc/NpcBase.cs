using Compendium.Invisibility;
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

using UnityEngine;

namespace Compendium.Npc
{
    public class NpcBase : Poolable, INpc
    {
        private ReferenceHub m_Hub;
        private Scp079Camera m_Camera;

        private ITarget m_Target;

        private NpcMovementMode m_MoveMode;
        private NpcMovementMode? m_ForcedMode;

        private bool m_IsSpawned;
        private bool m_PropsSet;
        private bool m_EnableScp079;
        private bool m_MovReg;
        private string m_CustomId;
        private float m_CurrentSpeed;
        private float? m_ForcedSpeed;

        private readonly Dictionary<NpcMovementMode, float> m_Distancing = new Dictionary<NpcMovementMode, float>()
        {
            [NpcMovementMode.Running] = 8f,
            [NpcMovementMode.Walking] = 5f,
            [NpcMovementMode.Teleport] = 10f
        };

        private readonly Dictionary<NpcMovementMode, float> m_Speed = new Dictionary<NpcMovementMode, float>()
        {
            [NpcMovementMode.Running] = 10f,
            [NpcMovementMode.Walking] = 5f,
            [NpcMovementMode.Teleport] = 0f
        };

        public ReferenceHub Hub => m_Hub;
        public Scp079Camera Camera { get => m_Camera; set => m_Camera = value; }

        public ITarget Target { get => m_Target; set => m_Target = value; }

        public Vector3 Position => IsSpawned ? m_Hub.transform.position : Vector3.zero;
        public Vector3 Rotation => IsSpawned ? m_Hub.transform.rotation.eulerAngles : Vector3.zero;

        public RoleTypeId RoleId { get => m_Hub.GetRoleId(); set => m_Hub.roleManager.ServerSetRole(value, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.None); }

        public PlayerRoleBase Role { get => m_Hub.roleManager.CurrentRole; set => m_Hub.roleManager.CurrentRole = value; }

        public NpcMovementMode CurMovementMode => m_MoveMode;
        public NpcMovementMode? ForcedMode => m_ForcedMode;

        public Dictionary<NpcMovementMode, float> Distancing => m_Distancing;
        public Dictionary<NpcMovementMode, float> Speed => m_Speed;

        public bool IsSpawned => m_IsSpawned;

        public bool Enable079Logic { get => m_EnableScp079; set => m_EnableScp079 = value; }

        public string Nick { get => m_Hub.nicknameSync.MyNick; set => m_Hub.nicknameSync.MyNick = value; }
        public string UserId { get => m_Hub.characterClassManager._privUserId; set => m_Hub.characterClassManager._privUserId = value; }
        public string CustomId { get => m_CustomId; set => m_CustomId = value; }

        public int Id { get => m_Hub._playerId.Value; set => m_Hub.Network_playerId = new RecyclablePlayerId(value); }

        public float CurrentSpeed => m_CurrentSpeed;
        public float? ForcedSpeed { get => m_ForcedSpeed; set => m_ForcedSpeed = value; }

        public NpcBase()
        {
            NpcManager.OnNpcCreated(this);
        }

        public void Despawn()
        {
            if (!IsSpawned) 
                return;

            m_IsSpawned = false;

            m_Target = null;
            m_ForcedMode = null;
            m_ForcedSpeed = null;
            m_Camera = null;

            m_Hub.MakeInvisible();

            NpcManager.OnNpcDespawned(this);

            Plugin.Info($"NPC despawned '{CustomId}'");
        }

        public virtual void Destroy()
        {
            m_Hub.OnDestroy();

            NpcManager.NpcHubs.Remove(m_Hub);

            CustomNetworkManager.TypedSingleton?.OnServerDisconnect(m_Hub.connectionToClient);

            UnityEngine.Object.Destroy(m_Hub.gameObject);

            m_Target = null;
            m_ForcedMode = null;
            m_ForcedSpeed = null;
            m_Hub = null;
            m_PropsSet = false;

            Plugin.Info($"NPC destroyed '{CustomId}'");

            CustomId = null;
        }

        public void Move(Vector3 destination)
        {
            m_Target = new PositionTarget(destination);
        }

        public virtual void Spawn(Action<INpc> modify = null)
        {
            try
            {
                if (m_Hub is null)
                {
                    m_Hub = NpcManager.NewHub;
                    m_PropsSet = false;
                }

                if (m_Hub is null)
                {
                    PluginAPI.Core.Log.Error($"Failed to instantiate a new ReferenceHub prefab!", "Npc Base");
                    m_PropsSet = false;
                    return;
                }

                if (!m_MovReg)
                    m_MovReg = Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", MovementLogic);

                modify?.Invoke(this);

                if (!m_PropsSet)
                {
                    NetworkServer.AddPlayerForConnection(new NpcConnection(Id), m_Hub.gameObject);

                    try
                    {
                        m_Hub.characterClassManager._privUserId = "npc@server";
                    }
                    catch { }

                    m_Hub.characterClassManager._targetInstanceMode = ClientInstanceMode.DedicatedServer;

                    try
                    {
                        m_Hub.nicknameSync.MyNick = $"NPC [{Id}]";
                    }
                    catch { }

                    CustomId = RandomGeneration.Default.GetRandom(1, 100).ToString();

                    m_PropsSet = true;
                    m_IsSpawned = true;

                    NpcManager.OnNpcSpawned(this);

                    Calls.Delay(0.2f, () =>
                    {
                        m_Hub.roleManager.ServerSetRole(RoleTypeId.ClassD, RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
                    });

                    Plugin.Info($"NPC created '{CustomId}'");
                }

                if (!m_IsSpawned)
                {
                    m_Hub.MakeVisible();
                    NpcManager.OnNpcSpawned(this);

                    Plugin.Info($"NPC spawned '{CustomId}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Error(ex);
            }
        }

        public void Teleport(Vector3 location)
        {
            if (!IsSpawned)
                return;

            m_Hub.TryOverridePosition(location, Rotation);
        }

        private NpcMovementMode GetMovementMode()
        {
            if (m_ForcedMode.HasValue)
                return m_ForcedMode.Value;

            var distance = DistanceToTarget();
            if (distance >= m_Distancing[NpcMovementMode.Running])
                return NpcMovementMode.Teleport;
            else if (distance >= m_Distancing[NpcMovementMode.Walking])
                return NpcMovementMode.Running;
            else
                return NpcMovementMode.Walking;
        }

        private float DistanceToTarget() => Vector3.Distance(Position, Target.Position);
        private float SpeedForMode(NpcMovementMode mode)
        {
            if (m_ForcedSpeed.HasValue)
                return m_ForcedSpeed.Value;

            if (!m_Speed.TryGetValue(mode, out var speed))
                speed = 5f;

            return speed;
        }

        private void MovementLogic()
        {
            if (!IsSpawned || m_Hub is null || !m_Hub.IsAlive())
                return;

            var hasTarget = m_Target != null && m_Target.IsValid;

            if (RoleId is RoleTypeId.Scp079)
            {
                if (m_EnableScp079)
                    UpdateScp079(hasTarget);

                return;
            }

            UpdateMovement(hasTarget);
            UpdateCamera(hasTarget);
        }

        public virtual void UpdateCamera(bool hasTarget)
        {
            if (!hasTarget)
                return;

            var rotation = Quaternion.LookRotation(m_Hub.transform.forward, m_Hub.transform.up) * m_Target.Position;
            NpcHelper.ForceRotation(m_Hub, rotation.x, rotation.y);
        }

        public virtual void UpdateMovement(bool hasTarget)
        {
            if (!hasTarget)
                return;

            var mode = GetMovementMode();
            var speed = SpeedForMode(mode);

            m_MoveMode = mode;
            m_CurrentSpeed = speed;

            if (m_MoveMode is NpcMovementMode.Teleport)
            {
                Teleport(m_Target.Position);
                return;
            }

            var hubMoveState = NpcHelper.TranslateMode(m_MoveMode);
            var position = (m_Hub.transform.position + m_Target.Position) * speed;

            NpcHelper.ForceState(m_Hub, hubMoveState);
            NpcHelper.ForceMove(m_Hub, position);
        }

        public virtual void UpdateScp079(bool hasTarget)
        {
            if (!hasTarget)
                return;

            var scp = m_Hub.roleManager.CurrentRole as Scp079Role;

            if (scp is null)
                return;

            if (scp.SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out var aux))
            {
                if (aux.CurrentAux < aux.MaxAux)
                {
                    aux.CurrentAux = aux.MaxAux;
                }
            }

            var closestCamera = NpcHelper.GetClosestCamera(m_Target.Position);

            if (closestCamera is null)
                return;

            if (m_Camera != null)
            {
                if (closestCamera.GetInstanceID() == m_Camera.GetInstanceID())
                    return;
            }

            m_Camera = closestCamera;

            scp._curCamSync._lastCam = m_Camera;
            scp._curCamSync.ClientSwitchTo(closestCamera);

            var rotation = Quaternion.LookRotation(m_Camera.transform.forward, m_Camera.transform.up) * m_Target.Position;

            m_Camera.HorizontalRotation = rotation.x;
            m_Camera.VerticalRotation = rotation.y;
            m_Camera.RollRotation = rotation.z;
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
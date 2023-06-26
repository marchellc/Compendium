using Compendium.Helpers.Events;
using Compendium.State.Interfaced;
using Compendium.State;
using Compendium.Extensions;

using helpers;
using helpers.Extensions;

using System;
using System.Collections.Generic;

using UnityEngine;

using PluginAPI.Events;
using PluginAPI.Enums;

using PlayerStatsSystem;
using PlayerRoles;

using Log = PluginAPI.Core.Log;
using Object = UnityEngine.Object;

namespace Compendium.Components
{
    public class StateController : MonoBehaviour
    {
        private static readonly Dictionary<uint, StateController> m_Controllers = new Dictionary<uint, StateController>();

        public static bool TryGetController(uint netId, out StateController controller) => m_Controllers.TryGetValue(netId, out controller);
        public static bool TryGetController(GameObject gameObject, out StateController controller) => gameObject.TryGet(out controller);

        public static void Destroy(uint netId)
        {
            if (TryGetController(netId, out var controller))
            {
                Object.Destroy(controller);
            }
        }

        public static void Destroy(GameObject gameObject)
        {
            if (TryGetController(gameObject, out var controller))
            {
                Object.Destroy(controller);
            }
        }

        public static void Initialize()
        {
            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent>>(OnJoined);
        }

        private static void OnJoined(PlayerJoinedEvent ev)
        {
            Plugin.Debug($"Player joined: {ev.Player.Nickname} ({ev.Player.UserId})");
            ev.Player.GameObject.AddComponent<StateController>();
        }

        public static StateController GetOrAdd(ReferenceHub hub) => hub.GetOrAddComponent<StateController>();

        private readonly List<IState> m_States = new List<IState>();
        private readonly Dictionary<string, CustomTimeIntervalStateData> m_Intervals = new Dictionary<string, CustomTimeIntervalStateData>();

        private ReferenceHub m_Owner;

        void Start()
        {
            if (!StateExtensions.IsReady)
            {
                throw new InvalidOperationException($"The state extensions have not been loaded yet!");
            }

            m_Owner = ReferenceHub.GetHub(gameObject);
            m_Controllers[m_Owner.netId] = this;

            ServerEventType.PlayerDamage.AddHandler<Action<PlayerDamageEvent>>(DamageHandler);
            ServerEventType.PlayerDeath.AddHandler<Action<PlayerDeathEvent>>(DeathHandler);
            ServerEventType.PlayerSpawn.AddHandler<Action<PlayerSpawnEvent>>(RoleChangeHandler);

            foreach (var requiredType in StateExtensions.RequiredStates)
            {
                AddState(requiredType);
            }    
        }

        void Update()
        {
            foreach (var state in m_States)
            {
                if (state is null)
                    continue;

                if (!state.IsActive)
                    continue;

                if (state.Player is null)
                    state.SetPlayer(m_Owner);

                if (state is ICustomUpdateTimeState timeState)
                {
                    if (m_Intervals.TryGetValue(state.Name, out var interval))
                    {
                        if (interval.CanUpdate())
                        {
                            if (UpdateState(state))
                                interval.OnUpdate(timeState.UpdateInterval);

                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        m_Intervals[state.Name] = new CustomTimeIntervalStateData(timeState.UpdateInterval);
                        continue;
                    }
                }
                else
                {
                    UpdateState(state);
                }
            }

            m_States.RemoveAll(s => s is null);
        }

        void OnDestroy()
        {
            m_Controllers.Remove(m_Owner.netId);
            m_Owner = null;

            ServerEventType.PlayerDamage.RemoveHandler<Action<PlayerDamageEvent>>(DamageHandler);
            ServerEventType.PlayerDeath.RemoveHandler<Action<PlayerDeathEvent>>(DeathHandler);
            ServerEventType.PlayerSpawn.RemoveHandler<Action<PlayerSpawnEvent>>(RoleChangeHandler);
        }

        bool UpdateState(IState state)
        {
            try
            {
                state.Update();
                StateExtensions.OnStateUpdated.Invoke(state);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to update state {state.Name} of player {m_Owner.nicknameSync.MyNick} ({m_Owner.characterClassManager.UserId}):\n{ex}", "State Controller");
                return false;
            }
        }

        bool HandleDeath(IState state, DamageHandlerBase damageHandler)
        {
            try
            {
                state.HandlePlayerDeath(damageHandler);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle death for state {state.Name} of player {m_Owner.nicknameSync.MyNick} ({m_Owner.characterClassManager.UserId}):\n{ex}", "State Controller");
                return false;
            }
        }

        bool HandleDamage(IState state, DamageHandlerBase damageHandler)
        {
            try
            {
                state.HandlePlayerDamage(damageHandler);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle damage for state {state.Name} of player {m_Owner.nicknameSync.MyNick} ({m_Owner.characterClassManager.UserId}):\n{ex}", "State Controller");
                return false;
            }
        }

        bool HandleRoleChange(IState state, RoleTypeId newRole)
        {
            try
            {
                state.HandlePlayerSpawn(newRole);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to handle role change for state {state.Name} of player {m_Owner.nicknameSync.MyNick} ({m_Owner.characterClassManager.UserId}):\n{ex}", "State Controller");
                return false;
            }
        }

        public void SetActive<TState>(bool active) where TState : IState
        {
            if (TryGetState<TState>(out var state))
            {
                var prevActive = state.IsActive;
                state.SetActive(active);

                if (!prevActive && active)
                    state.Load();
                else if (prevActive && !active)
                    state.Unload();
            }
        }

        public void SetActive(string stateId, bool active)
        {
            if (TryGetState(stateId, out var state))
            {
                var prevActive = state.IsActive;

                state.SetActive(active);

                if (!prevActive && active)
                    state.Load();
                else if (prevActive && !active)
                    state.Unload();
            }
        }

        public void SetActive(Type type, bool active)
        {
            if (TryGetState(type, out var state))
            {
                var prevActive = state.IsActive;

                state.SetActive(active);

                if (!prevActive && active)
                    state.Load();
                else if (prevActive && !active)
                    state.Unload();
            }
        }

        public IState[] GetStates() => m_States.ToArray();

        public IState GetOrAddState(Type type) => TryGetState(type, out var state) ? state : (state = AddState(type));
        public TState GetOrAddState<TState>() where TState : IState => TryGetState<TState>(out var state) ? state : (state = AddState<TState>());

        public TState GetState<TState>() where TState : IState => TryGetState<TState>(out var state) ? state : default;
        public IState GetState(IState state) => TryGetState(state, out var res) ? res : throw new KeyNotFoundException($"Same instance does not exist in this manager.");
        public IState GetState(Type type) => TryGetState(type, out var state) ? state : throw new KeyNotFoundException(type.FullName);
        public IState GetState(string stateId) => TryGetState(stateId, out var state) ? state : throw new KeyNotFoundException(stateId);

        public bool TryGetState<TState>(out TState state) where TState : IState
        {
            if (TryGetState(typeof(TState), out var result))
            {
                if (result is TState res)
                {
                    state = res;
                    return true;
                }
            }

            state = default;
            return false;
        }

        public bool TryGetStates(out IState[] states)
        {
            states = m_States.ToArray();
            return states.Any();
        }

        public bool TryGetState(IState state, out IState resultState) => m_States.TryGetFirst(s => s == state, out resultState);
        public bool TryGetState(Type type, out IState state) => m_States.TryGetFirst(s => s.GetType() == type, out state);
        public bool TryGetState(string stateId, out IState state) => m_States.TryGetFirst(s => s.Name == stateId, out state);

        public void RemoveAll()
        {
            m_States.ForEach(state =>
            {
                state.SetActive(false);
                state.Unload();
                state.SetPlayer(null);
            });

            m_States.Clear();
        }

        public void RemoveState<TState>() where TState : IState
        {
            if (TryGetState<TState>(out var state))
            {
                state.SetActive(false);
                state.Unload();
                state.SetPlayer(null);
            }

            m_States.RemoveAll(s => s.GetType() == typeof(TState));
        }

        public void RemoveState(IState state)
        {
            state.SetActive(false);
            state.Unload();
            state.SetPlayer(null);

            m_States.RemoveAll(s => s == state);
        }

        public void RemoveState(Type type)
        {
            if (TryGetState(type, out var state))
            {
                state.SetActive(false);
                state.Unload();
                state.SetPlayer(null);
            }

            m_States.RemoveAll(s => s.GetType() == type);
        }

        public void RemoveState(string stateId)
        {
            if (TryGetState(stateId, out var state))
            {
                state.SetActive(false);
                state.Unload();
                state.SetPlayer(null);
            }

            m_States.RemoveAll(s => s.Name == stateId);
        }

        public TState AddState<TState>() where TState : IState
        {
            var state = Reflection.Instantiate<TState>() as IState;
            if (state is null)
                return default;

            return AddState<TState>().As<TState>();
        }

        public IState AddState(Type type)
        {
            var state = Reflection.Instantiate<IState>(type);
            if (state is null)
                return null;

            return AddState(state);
        }

        public IState AddState(IState state)
        {
            if (TryGetState(state.Name, out _))
            {
                Log.Warning($"Attemped to add a duplicate state: {state.Name}", "State Controller");
                return null;
            }

            m_States.Add(state);

            state.SetPlayer(m_Owner);
            state.SetActive(true);
            state.Load();

            m_Owner.gameConsoleTransmission.SendToClient(m_Owner.connectionToClient, $"[State Controller] State controller added: {state.Name}", "green");

            StateExtensions.OnStateAdded.Invoke(state);

            return state;
        }

        private void DeathHandler(PlayerDeathEvent ev)
        {
            if (ev.Player is null || ev.Player.IsServer)
                return;

            if (m_Owner is null)
                return;

            if (ev.Player.NetworkId != m_Owner.netId)
                return;

            foreach (var state in m_States)
            {
                HandleDeath(state, ev.DamageHandler);
            }

            m_States.RemoveAll(s => s.Flags.HasFlag(StateFlags.RemoveOnDeath));
        }

        private void DamageHandler(PlayerDamageEvent ev)
        {
            if (ev.Player is null || ev.Player.IsServer)
                return;

            if (m_Owner is null)
                return;

            if (ev.Player.NetworkId != m_Owner.netId)
                return;

            foreach (var state in m_States)
            {
                HandleDamage(state, ev.DamageHandler);
            }

            m_States.RemoveAll(s => s.Flags.HasFlag(StateFlags.RemoveOnDamage));
        }

        private void RoleChangeHandler(PlayerSpawnEvent ev)
        {
            if (ev.Player is null || ev.Player.IsServer)
                return;

            if (m_Owner is null)
                return;

            if (ev.Player.NetworkId != m_Owner.netId)
                return;

            foreach (var state in m_States)
            {
                HandleRoleChange(state, ev.Role);
            }

            m_States.RemoveAll(s => s.Flags.HasFlag(StateFlags.RemoveOnRoleChange));
        }
    }
}
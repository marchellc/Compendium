using Compendium.Attributes;
using Compendium.Helpers.Events;
using Compendium.Helpers.Timing;
using Compendium.State.Base;
using Compendium.State.Interfaced;

using helpers;
using helpers.Events;
using helpers.Extensions;
using helpers.Pooling.Pools;

using PlayerRoles;
using PlayerStatsSystem;

using PluginAPI.Core;
using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.State
{
    [LogSource("State Controller")]
    public static class StateController
    {
        private static readonly List<Type> _requiredStates = new List<Type>();
        private static readonly List<Type> _knownStates = new List<Type>();

        private static readonly Dictionary<uint, HashSet<IState>> _playerStates = new Dictionary<uint, HashSet<IState>>();
        private static readonly Dictionary<string, CustomTimeIntervalStateData> _delayedExecutions = new Dictionary<string, CustomTimeIntervalStateData>();

        public static readonly Type IStateInterfaceType = typeof(IState);
        public static readonly Type IRequiredStateInterfaceType = typeof(IRequiredState);

        public static readonly EventProvider OnStateAdded = new EventProvider();
        public static readonly EventProvider OnStateRemoved = new EventProvider();
        public static readonly EventProvider OnStateUpdated = new EventProvider();

        [InitOnLoad]
        public static void Initialize()
        {
            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (type.IsAssignableFrom(IStateInterfaceType)) _knownStates.Add(type);
                if (type.IsAssignableFrom(IRequiredStateInterfaceType)) _requiredStates.Add(type);
            }

            ServerEventType.PlayerJoined.GetProvider()?.Add(OnPlayerJoined);
            ServerEventType.PlayerLeft.GetProvider()?.Add(OnPlayerLeft);
            ServerEventType.PlayerDamage.GetProvider()?.Add(OnPlayerDamaged);
            ServerEventType.PlayerSpawn.GetProvider()?.Add(OnPlayerSpawned);
            ServerEventType.PlayerDeath.GetProvider()?.Add(OnPlayerDied);

            FrameUpdateHelper.OnUpdateEvent.Add(OnUpdate);
        }

        public static TState GetState<TState>(this ReferenceHub hub) where TState : IState => GetState(hub, typeof(TState)).As<TState>();
        public static TState GetOrAddState<TState>(this ReferenceHub hub) where TState : IState => TryGetState<TState>(hub, out var state) ? state : (state = AddState(hub, typeof(TState)).As<TState>());
        public static IState GetOrAddState(this ReferenceHub hub, Type type) => TryGetState(hub, type, out var state) ? state : (state = hub.AddState(type));
        public static IState GetState(this ReferenceHub hub, Type type) => _playerStates.TryGetValue(hub.netId, out var states) ? states.FirstOrDefault(x => x.GetType() == type) : null;

        public static bool TryGetState<TState>(this ReferenceHub hub, out TState state) where TState : IState => TryGetState(hub, typeof(TState), out var baseState) ? (state = baseState.As<TState>()) != null : (state = default) != null;
        public static bool TryGetStates(this ReferenceHub hub, out IState[] states) => _playerStates.TryGetValue(hub.netId, out var stateList) ? (states = stateList.ToArray()) != null : (states = null) != null;
        public static bool TryGetState(this ReferenceHub hub, Type type, out IState state)
        {
            if (_playerStates.TryGetValue(hub.netId, out var states))
            {
                state = states.FirstOrDefault(x => x.GetType() == type);
                return state != null;
            }

            state = null;
            return false;
        }

        public static TState[] GetStates<TState>(this ReferenceHub hub) where TState : IState => _playerStates.TryGetValue(hub.netId, out var states) ? states.Where(x => x is TState).Select(y => y.As<TState>()).ToArray() : null;
        public static IState[] GetStates(this ReferenceHub hub, params Type[] types) => _playerStates.TryGetValue(hub.netId, out var states) ? states.Where(x => types.Contains(x.GetType())).ToArray() : null;

        public static void RemoveState<TState>(this ReferenceHub hub) where TState : IState => RemoveState(hub, _playerStates[hub.netId].First(x => x is TState));
        public static void RemoveStates(this ReferenceHub hub, params Type[] types) => types.ForEach(x => hub.RemoveState(_playerStates[hub.netId].First(y => y.GetType() == x)));
        public static void RemoveState(this ReferenceHub hub, IState state)
        {
            if (_playerStates.TryGetValue(hub.netId, out var states) && states.Remove(state))
            {
                NotifyPlayer(hub, state.Name, false);
            }
        }

        public static TState AddState<TState>(this ReferenceHub hub) where TState : IState, new() { if (!HasState<TState>(hub)) return AddState(hub, new TState()).As<TState>(); else return GetState<TState>(hub); }
        public static IState[] AddStates(this ReferenceHub hub, params Type[] types) => AddStates(hub, types.Select(x => Reflection.Instantiate<IState>(x)).ToArray());
        public static IState AddState(this ReferenceHub hub, Type type) => AddState(hub, Reflection.Instantiate<IState>(type));
        public static IState[] AddStates(this ReferenceHub hub, params IState[] states) { states.ForEach(x => hub.AddState(x)); return states; }
        public static IState AddState(this ReferenceHub hub, IState state)
        {
            if (!_playerStates.TryGetValue(hub.netId, out var states)) states = (_playerStates[hub.netId] = new HashSet<IState>());
            if (states.Any(x => x.GetType() == state.GetType())) return null;

            states.Add(state);
            state.SetPlayer(hub);
            state.Load();
            state.Enable();

            NotifyPlayer(hub, state.Name, true);

            return state;
        }

        public static bool HasState<TState>(this ReferenceHub hub) where TState : IState => HasState(hub, typeof(TState));
        public static bool HasState(this ReferenceHub hub, Type type) => _playerStates.TryGetValue(hub.netId, out var states) && states.Any(x => x.GetType() == type);

        private static void NotifyPlayer(ReferenceHub hub, string name, bool addedOrRemoved)
        {
            if (addedOrRemoved) hub.characterClassManager.ConsolePrint($"[State Controller] Added a new player state: {name}", "green");
            else hub.characterClassManager.ConsolePrint($"[State Controller] Removed a controller state: {name}", "red");
        }

        private static void OnUpdate()
        {
            foreach (var states in _playerStates.Values)
            {
                foreach (var state in states)
                {
                    if (state.Flags.HasFlag(StateFlags.DisableUpdate)) continue;
                    if (state is ICustomUpdateTimeState customUpdateTimeState)
                    {
                        if (!_delayedExecutions.TryGetValue(state.Name, out var customTimeIntervalStateData)) customTimeIntervalStateData = (_delayedExecutions[state.Name] = new CustomTimeIntervalStateData(customUpdateTimeState.UpdateInterval));
                        if (!customTimeIntervalStateData.CanUpdate()) continue;

                        state.Update();
                        customTimeIntervalStateData.OnUpdate(customUpdateTimeState.UpdateInterval);
                    }
                    else
                    {
                        state.Update();
                    }
                }
            }
        }

        private static void OnPlayerSpawned(EventArgsCollection eventArgsCollection)
        {
            var hub = eventArgsCollection.Get<Player>("player").ReferenceHub;
            var removeList = ListPool<IState>.Pool.Get();

            if (hub.TryGetStates(out var states))
            {
                foreach (var state in states)
                {
                    state.HandlePlayerSpawn(eventArgsCollection.Get<RoleTypeId>("role"));
                    if (state.Flags.HasFlag(StateFlags.RemoveOnRoleChange))
                    {
                        state.Disable();
                        state.Unload();
                        removeList.Add(state);
                    }
                }
            }

            removeList.ForEach(x => RemoveState(x.Player, x));
            ListPool<IState>.Pool.Push(removeList);
        }

        private static void OnPlayerDied(EventArgsCollection eventArgsCollection)
        {
            var hub = eventArgsCollection.Get<Player>("player").ReferenceHub;
            var removeList = ListPool<IState>.Pool.Get();

            if (hub.TryGetStates(out var states))
            {
                foreach (var state in states)
                {
                    state.HandlePlayerDeath(eventArgsCollection.Get<DamageHandlerBase>("damageHandler"));
                    if (state.Flags.HasFlag(StateFlags.RemoveOnDeath))
                    {
                        state.Disable();
                        state.Unload();
                        removeList.Add(state);
                    }
                }    
            }

            removeList.ForEach(x => RemoveState(x.Player, x));
            ListPool<IState>.Pool.Push(removeList);
        }

        private static void OnPlayerDamaged(EventArgsCollection eventArgsCollection)
        {
            var hub = eventArgsCollection.Get<Player>("player").ReferenceHub;
            var removeList = ListPool<IState>.Pool.Get();

            if (hub.TryGetStates(out var states))
            {
                foreach (var state in states)
                {
                    state.HandlePlayerDamage(eventArgsCollection.Get<DamageHandlerBase>("damageHandler"));
                    if (state.Flags.HasFlag(StateFlags.RemoveOnDeath))
                    {
                        state.Disable();
                        state.Unload();
                        removeList.Add(state);
                    }
                }
            }

            removeList.ForEach(x => RemoveState(x.Player, x));
            ListPool<IState>.Pool.Push(removeList);
        }

        private static void OnPlayerLeft(EventArgsCollection eventArgsCollection)
        {
            var netId = eventArgsCollection.Get<Player>("player").NetworkId;

            if (_playerStates.TryGetValue(netId, out var states)) states.ForEach(x => { x.Disable(); x.Unload(); x.SetPlayer(null); } );
            _playerStates.Remove(netId);
        }

        private static void OnPlayerJoined(EventArgsCollection eventArgsCollection)
        {
            foreach (var stateType in _requiredStates)
            {
                var stateInstance = Reflection.Instantiate<StateBase>(stateType);
                if (stateInstance != null) AddState(eventArgsCollection.Get<Player>("player").ReferenceHub, stateInstance);
                else Plugin.Error($"Failed to create an instance of state type: {stateType.FullName}");
            }
        }
    }
}
using Compendium.Helpers.Events;

using helpers;
using helpers.Attributes;
using helpers.Extensions;

using PlayerRoles;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Helpers.Round
{
    public static class RoundHelper
    {
        private static RoundState _state;
        private static readonly Dictionary<MethodInfo, Tuple<bool, RoundState[]>> _onChanged = new Dictionary<MethodInfo, Tuple<bool, RoundState[]>>();

        public static RoundState State
        {
            get => _state;
            set
            {
                _state = value;

                Plugin.Debug($"Changed round state to {_state}");

                _onChanged.ForEach(pair =>
                {
                    try
                    {
                        if (pair.Value.Item2.IsEmpty() || pair.Value.Item2.Contains(_state))
                        {
                            if (pair.Value.Item1)
                                pair.Key.Invoke(null, new object[] { _state });
                            else
                                pair.Key.Invoke(null, null);
                        }
                    }
                    catch { }
                });
            }
        }

        public static bool IsStarted => State is RoundState.InProgress;
        public static bool IsEnding => State is RoundState.Ending;
        public static bool IsRestarting => State is RoundState.Restarting;
        public static bool IsWaitingForPlayers => State is RoundState.WaitingForPlayers;

        public static bool TryGenerateEndPreventingPlayerList(out List<ReferenceHub> hubs)
        {
            if (!IsStarted)
            {
                hubs = null;
                return false;
            }

            hubs = ReferenceHub.AllHubs.Where(hub => hub.Mode is ClientInstanceMode.ReadyClient && hub.IsAlive()).ToList();

            if (!hubs.Any())
                return false;

            if (hubs.Any(hub => hub.IsSCP()))
            {

            }

            return true;
        }

        internal static void ScanAssemblyForOnChanged(Assembly assembly)
        {
            assembly.ForEachType(type =>
            {
                type.ForEachMethod(method =>
                {
                    if (!method.IsStatic)
                        return;

                    if (!method.TryGetAttribute<RoundStateChangedAttribute>(out var roundStateChangedAttribute))
                        return;

                    var args = method.GetParameters();
                    var hasState = false;

                    if (args != null && args.Any())
                    {
                        if (args.Length != 1)
                            return;

                        if (args[0].ParameterType != typeof(RoundState))
                            return;

                        hasState = true;
                    }

                    _onChanged[method] = new Tuple<bool, RoundState[]>(hasState, roundStateChangedAttribute.TargetStates);
                });
            });
        }

        [Load]
        private static void Initialize()
        {
            ServerEventType.RoundStart.AddHandler<Action>(OnStart);
            ServerEventType.RoundRestart.AddHandler<Action>(OnRestart);
            ServerEventType.WaitingForPlayers.AddHandler<Action>(OnWaiting);
            ServerEventType.RoundEnd.AddHandler<Action>(OnEnd);

            ScanAssemblyForOnChanged(Assembly.GetExecutingAssembly());
        }

        [Unload]
        private static void Unload()
        {
            ServerEventType.RoundStart.RemoveHandler<Action>(OnStart);
            ServerEventType.RoundRestart.RemoveHandler<Action>(OnRestart);
            ServerEventType.WaitingForPlayers.RemoveHandler<Action>(OnWaiting);
            ServerEventType.RoundEnd.RemoveHandler<Action>(OnEnd);

            _onChanged.Clear();
        }

        private static void OnEnd()
            => State = RoundState.Ending;

        private static void OnStart()
            => State = RoundState.InProgress;

        private static void OnRestart()
            => State = RoundState.Restarting;

        private static void OnWaiting()
            => State = RoundState.WaitingForPlayers;
    }
}
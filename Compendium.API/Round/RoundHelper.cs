using Compendium.Events;

using helpers;
using helpers.Attributes;
using helpers.Extensions;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Compendium.Round
{
    public static class RoundHelper
    {
        private static RoundState _state;
        private static readonly List<Tuple<Delegate, bool, bool, bool, bool>> _onChanged = new List<Tuple<Delegate, bool, bool, bool, bool>>();

        public static RoundState State
        {
            get => _state;
            set
            {
                _state = value;
                _onChanged.For((_, pair) =>
                {
                    if (_state is RoundState.WaitingForPlayers && !pair.Item2)
                        return;

                    if (_state is RoundState.InProgress && !pair.Item3)
                        return;

                    if (_state is RoundState.Restarting && !pair.Item4)
                        return;

                    if (_state is RoundState.Ending && !pair.Item5)
                        return;

                    if (pair.Item1 is Action action)
                        Calls.DirectAction(action);
                    else if (pair.Item1 is Action<RoundState> act)
                        Calls.DirectAction(act, _state);
                });
            }
        }

        public static bool IsStarted => State is RoundState.InProgress;
        public static bool IsEnding => State is RoundState.Ending;
        public static bool IsRestarting => State is RoundState.Restarting;
        public static bool IsWaitingForPlayers => State is RoundState.WaitingForPlayers;
        public static bool IsReady => State != RoundState.Restarting;

        internal static void ScanAssemblyForOnChanged(Assembly assembly)
        {
            try
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

                        Delegate del = null;

                        if (hasState)
                            del = method.CreateDelegate(typeof(Action<RoundState>));
                        else
                            del = method.CreateDelegate(typeof(Action));

                        _onChanged.Add(new Tuple<Delegate, bool, bool, bool, bool>(
                            del,

                            roundStateChangedAttribute.TargetStates.IsEmpty() || roundStateChangedAttribute.TargetStates.Contains(RoundState.WaitingForPlayers),
                            roundStateChangedAttribute.TargetStates.IsEmpty() || roundStateChangedAttribute.TargetStates.Contains(RoundState.InProgress),
                            roundStateChangedAttribute.TargetStates.IsEmpty() || roundStateChangedAttribute.TargetStates.Contains(RoundState.Restarting),
                            roundStateChangedAttribute.TargetStates.IsEmpty() || roundStateChangedAttribute.TargetStates.Contains(RoundState.Ending)));
                    });
                });
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to find round-state attributes in assembly: {assembly.FullName}");
                Plugin.Error(ex);
            }
        }

        [Unload]
        private static void Unload()
        {
            _onChanged.Clear();
        }

        [Event(ServerEventType.RoundEnd)]
        private static void OnEnd()
            => State = RoundState.Ending;

        [Event(ServerEventType.RoundStart)]
        private static void OnStart()
            => State = RoundState.InProgress;

        [Event(ServerEventType.RoundRestart)]
        private static void OnRestart()
            => State = RoundState.Restarting;

        [Event(ServerEventType.WaitingForPlayers)]
        private static void OnWaiting()
            => State = RoundState.WaitingForPlayers;
    }
}
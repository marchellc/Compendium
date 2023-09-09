using Compendium.Events;

using helpers;
using helpers.Attributes;
using helpers.Extensions;

using PlayerRoles;

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
        private static readonly Dictionary<MethodInfo, Tuple<bool, RoundState[]>> _onChanged = new Dictionary<MethodInfo, Tuple<bool, RoundState[]>>();

        public static RoundState State
        {
            get => _state;
            set
            {
                _state = value;
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

                        _onChanged[method] = new Tuple<bool, RoundState[]>(hasState, roundStateChangedAttribute.TargetStates);
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
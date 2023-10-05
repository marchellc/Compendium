using Compendium.Events;
using Compendium.Enums;

using helpers.Dynamic;
using helpers;

using PluginAPI.Enums;

using System;
using System.Reflection;
using System.Linq;

using Compendium.Attributes;

using helpers.Attributes;

namespace Compendium
{
    public static class RoundHelper
    {
        private static RoundState _state;
        private static object[] _stateArgs;

        public static readonly RoundState[] States = Enum.GetValues(typeof(RoundState)).Cast<RoundState>().ToArray();

        public static RoundState State
        {
            get => _state;
            set
            {
                if (_stateArgs is null)
                    _stateArgs = new object[1];

                _state = value;
                _stateArgs[0] = value;

                AttributeRegistry<RoundStateChangedAttribute>.ForEachOfCondition((data, attribute) =>
                {
                    if (attribute.Data is null)
                        return false;

                    var stateIndex = States.IndexOf(_state);

                    if (!(bool)attribute.Data[stateIndex + 1])
                        return false;

                    return true;
                }, 
                
                attribute =>
                {
                    if (attribute.Member != null && attribute.Member is MethodInfo method)
                        method.InvokeDynamic(attribute.MemberHandle, (bool)attribute.Data[0] ? _stateArgs : CachedArray.EmptyObject);
                });
            }
        }

        public static bool IsStarted => State is RoundState.InProgress;
        public static bool IsEnding => State is RoundState.Ending;
        public static bool IsRestarting => State is RoundState.Restarting;
        public static bool IsWaitingForPlayers => State is RoundState.WaitingForPlayers;
        public static bool IsReady => State != RoundState.Restarting;

        [Load]
        private static void Load()
            => AttributeRegistry<RoundStateChangedAttribute>.DataGenerator = AttributeDataGenerator;

        [Event(ServerEventType.RoundEnd)]
        private static void OnEnd() => State = RoundState.Ending;
        [Event(ServerEventType.RoundStart)]
        private static void OnStart() => State = RoundState.InProgress;
        [Event(ServerEventType.RoundRestart)]
        private static void OnRestart() => State = RoundState.Restarting;
        [Event(ServerEventType.WaitingForPlayers)]
        private static void OnWaiting() => State = RoundState.WaitingForPlayers;

        private static object[] AttributeDataGenerator(Type type, MemberInfo member, RoundStateChangedAttribute attribute)
        {
            if (member is null || !(member is MethodInfo method))
                return null;

            var array = new object[1 + States.Length];
            var pars = method.GetParameters();

            array[0] = pars != null && pars.Length > 0;

            for (int i = 0; i < States.Length; i++)
                array[i + 1] = attribute.TargetStates.IsEmpty() || attribute.TargetStates.Contains(States[i]);

            return array;
        }
    }
}
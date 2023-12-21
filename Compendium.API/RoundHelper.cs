using Compendium.Events;
using Compendium.Enums;
using Compendium.Extensions;
using Compendium.Attributes;

using helpers.Attributes;
using helpers.Dynamic;
using helpers;

using System;
using System.Reflection;
using System.Linq;

using PlayerRoles;

using PluginAPI.Core;
using PluginAPI.Enums;

using PluginAPI.Events;

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

        // original by Mallifrey
        public static ReferenceHub[] GetLastPlayers(bool isHumanPriority = true)
        {
            if (State != RoundState.InProgress)
                return CachedArray<ReferenceHub>.Array;

            var list = Pools.PoolList<ReferenceHub>();
            var foundationForces = Hub.GetHubs(Team.Scientists).Count() + Hub.GetHubs(Team.FoundationForces).Count();
            var classDs = Hub.GetHubs(RoleTypeId.ClassD).Count();
            var chaosTargets = RoundSummary.singleton?.ChaosTargetCount ?? 0;
            var scps = Hub.GetHubs(Team.SCPs).Count();
            var deadFaction = Faction.FoundationEnemy;
            var totalTeams = 0;

            if (foundationForces > 0)
                totalTeams++;

            if (classDs > 0 || chaosTargets > 0)
                totalTeams++;

            if (scps > 0)
                totalTeams++;

            if (foundationForces <= 0)
                deadFaction = Faction.FoundationStaff;
            else if (scps <= 0)
                deadFaction = Faction.SCP;

            if (!(totalTeams == 2 && (foundationForces == 1 || (chaosTargets == 1 && classDs == 0) || (classDs == 1 && chaosTargets == 0) || scps == 1)))
            {
                list.ReturnList();
                return CachedArray<ReferenceHub>.Array;
            }

            switch (deadFaction)
            {
                case Faction.SCP:
                    {
                        if (Respawn.NtfTickets < 0.5f && foundationForces == 1)
                            list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
                        else if ((classDs == 0 && chaosTargets == 1) || (classDs == 1 && chaosTargets == 0))
                            list.Add(Hub.GetHubs(Faction.FoundationEnemy).First());
                        else if (foundationForces == 1)
                            list.Add(Hub.GetHubs(Faction.FoundationStaff).First());

                        break;
                    }

                case Faction.FoundationEnemy:
                    {
                        if (isHumanPriority && foundationForces == 1)
                            list.Add(Hub.GetHubs(Faction.FoundationStaff).First());
                        else if (scps == 1)
                            list.Add(Hub.GetHubs(Team.SCPs).First());
                        else if (foundationForces == 1)
                            list.Add(Hub.GetHubs(Faction.FoundationStaff).First());

                        break;
                    }

                case Faction.FoundationStaff:
                    {
                        if (!isHumanPriority && scps == 1)
                            list.Add(Hub.GetHubs(Team.SCPs).First());
                        else if (chaosTargets == 1 && classDs == 0)
                            list.Add(PickChaosTargetByDistance());
                        else if (chaosTargets == 0 && classDs == 1)
                            list.Add(Hub.GetHubs(Team.ClassD).First());
                        else if (scps == 1)
                            list.Add(Hub.GetHubs(Team.SCPs).First());

                        break;
                    }
            }

            var array = list.ToArray();
            list.ReturnList();
            return array;
        }

        public static ReferenceHub PickChaosTargetByDistance()
        {
            var chaos = Hub.GetHubs(Team.ChaosInsurgency);

            if (chaos.Count() <= 0)
                return null;

            var scps = Hub.GetHubs(Team.SCPs);
            var dist = Pools.PoolList<Tuple<ReferenceHub, ReferenceHub, float>>();

            foreach (var chaosPlayer in chaos)
                foreach (var scpPlayer in scps)
                    dist.Add(new Tuple<ReferenceHub, ReferenceHub, float>(scpPlayer, chaosPlayer, chaosPlayer.Position().DistanceSquared(scpPlayer.Position())));

            var chosen = dist.OrderBy(d => d.Item3).FirstOrDefault();

            dist.ReturnList();
            return chosen?.Item2 ?? null;
        }

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
        private static void OnWaiting()
        {
            State = RoundState.WaitingForPlayers;
        }

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
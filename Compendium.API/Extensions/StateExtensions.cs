using Compendium.Components;
using Compendium.State.Base;
using Compendium.State.Interfaced;

using helpers;
using helpers.Attributes;
using helpers.Events;

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Compendium.Extensions
{
    public static class StateExtensions
    {
        private static readonly List<Type> _requiredStates = new List<Type>();
        private static readonly List<Type> _knownStates = new List<Type>();

        public static readonly EventProvider OnStateAdded = new EventProvider();
        public static readonly EventProvider OnStateRemoved = new EventProvider();
        public static readonly EventProvider OnStateUpdated = new EventProvider();

        public static IReadOnlyList<Type> AllStates => _knownStates;
        public static IReadOnlyList<Type> RequiredStates => _requiredStates;

        public static bool IsReady { get; private set; }

        [Load]
        public static void Initialize()
        {
            foreach (var type in Assembly
                .GetExecutingAssembly()
                .GetTypes())
            {
                if (Reflection.HasInterface<IState>(type)
                    && type != typeof(StateBase)
                    && type != typeof(CustomUpdateTimeStateBase)
                    && type != typeof(CustomRangedUpdateTimeState)
                    && type != typeof(RequiredStateBase))
                    _knownStates.Add(type);

                if (Reflection.HasInterface<IRequiredState>(type)
                    && type != typeof(StateBase)
                    && type != typeof(CustomUpdateTimeStateBase)
                    && type != typeof(CustomRangedUpdateTimeState)
                    && type != typeof(RequiredStateBase))
                    _requiredStates.Add(type);
            }

            IsReady = true;
        }

        public static bool TryGetController(this ReferenceHub hub, out StateController controller) => StateController.TryGetController(hub.netId, out controller);

        public static void ExecuteIfFound(this ReferenceHub hub, Action<StateController> action)
        {
            if (TryGetController(hub, out var controller))
            {
                action?.Invoke(controller);
            }
        }

        public static TState GetOrAddState<TState>(this ReferenceHub hub) where TState : IState
        {
            var controller = StateController.GetOrAdd(hub);
            if (controller.TryGetState<TState>(out var state))
            {
                return state;
            }
            else
            {
                return controller.AddState<TState>();
            }
        }
    }
}
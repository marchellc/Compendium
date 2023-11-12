using Compendium.Events;

using helpers;
using helpers.CustomReflect;
using helpers.Extensions;

using MEC;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Compendium.Updating
{
    public static class UpdateHandler
    {
        private static List<UpdateData> _updates;
        private static CoroutineHandle _cor;

        private static volatile ConcurrentQueue<UpdateData> _updatesQueue;

        public static Thread Thread;

        static UpdateHandler()
        {
            _updates = new List<UpdateData>();
            _updatesQueue = new ConcurrentQueue<UpdateData>();

            _cor = MEC.Timing.RunCoroutine(Handler());

            Thread = new Thread(async () =>
            {
                while (true)
                {
                    await Task.Delay(100);

                    while (_updatesQueue.TryDequeue(out var update))
                        update.DoCall();
                }
            });

            Thread.Start();
        }

        public static bool Unregister()
            => Unregister(Assembly.GetCallingAssembly());

        public static bool Unregister(Assembly assembly)
            => assembly.GetTypes().Any(t => Unregister(t, null));

        public static bool Unregister(Type type, object target)
            => type.GetMethods(Reflection.AllFlags).Any(m => Unregister(m, target));

        public static bool Unregister(Action handler)
            => Unregister(handler.Method, handler.Target);

        public static bool Unregister(Action<UpdateData> handler)
            => Unregister(handler.Method, handler.Target);

        public static bool Unregister(MethodInfo method, object target)
            => _updates.RemoveAll(u => u.Is(method, target)) > 0;

        public static void Register()
            => Register(Assembly.GetCallingAssembly());

        public static void Register(Assembly assembly)
            => assembly.ForEachType(t => Register(t, null));

        public static void Register(Type type, object target)
            => type.ForEachMethod(m =>
            {
                if (!m.IsDefined(typeof(UpdateAttribute)))
                    return;

                Register(m, target);
            });

        public static void Register(Action handler, bool isUnity = true, bool isWaiting = true, bool isRestarting = true, int delay = -1)
        {
            if (_updates.Any(u => u.Is(handler.Method, handler.Target)))
            {
                Plugin.Error($"Cannot register update method {handler.Method.ToLogName()}: already registered");
                return;
            }

            _updates.Add(new UpdateData(isUnity, isWaiting, isRestarting, delay, handler));

            Plugin.Info($"Registered update handler '{handler.Method.ToLogName()}' (delay: {delay} ms)");
        }

        public static void Register(Action<UpdateData> handler, bool isUnity = true, bool isWaiting = true, bool isRestarting = true, int delay = -1)
        {
            if (_updates.Any(u => u.Is(handler.Method, handler.Target)))
            {
                Plugin.Error($"Cannot register update method {handler.Method.ToLogName()}: already registered");
                return;
            }

            _updates.Add(new UpdateData(isUnity, isWaiting, isRestarting, delay, handler));

            Plugin.Info($"Registered update handler '{handler.Method.ToLogName()}' (delay: {delay} ms)");
        }

        public static void Register(MethodInfo method, object target)
        {
            if (_updates.Any(u => u.Is(method, target)))
            {
                Plugin.Error($"Cannot register update method {method.ToLogName()}: already registered");
                return;
            }

            if (!method.TryGetAttribute<UpdateAttribute>(out var update))
            {
                Plugin.Error($"Cannot register update method {method.ToLogName()}: missing attribute");
                return;
            }

            if (!EventUtils.TryValidateInstance(method, ref target))
            {
                Plugin.Error($"Cannot register update method {method.ToLogName()}: invalid target instance");
                return;
            }

            if (!update.IsUnity)
            {
                var instructions = MethodBodyReader.GetInstructions(method);
                var unityInstrc = new List<MethodBase>();

                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].Operand is MethodBase methodIns)
                    {
                        if (Reflection.HasType<UnityEngine.Object>(methodIns.DeclaringType)
                            || methodIns.DeclaringType.Assembly.FullName.Contains("Assembly-CSharp"))
                            unityInstrc.Add(methodIns);
                    }
                }

                if (unityInstrc.Count > 0)
                {
                    Plugin.Error($"Cannot register method {method.ToLogName()}: method running on separate thread contains Unity instructions");

                    for (int i = 0; i < unityInstrc.Count; i++)
                        Plugin.Error(unityInstrc[i].ToLogName());

                    return;
                }
            }

            var param = method.GetParameters();

            if (param.Length == 0)
            {
                var del = BuildDelegate<Action>(method, target);

                if (del is null)
                {
                    Plugin.Error($"Cannot register update method {method.ToLogName()}: invalid delegate");
                    return;
                }

                _updates.Add(new UpdateData(update.IsUnity, update.PauseWaiting, update.PauseRestarting, update.Delay, del));

                Plugin.Info($"Registered update '{method.ToLogName()}' (delay: {update.Delay} ms)");
            }
            else if (param.Length == 1 && param[0].ParameterType == typeof(UpdateData))
            {
                var del = BuildDelegate<Action<UpdateData>>(method, target);

                if (del is null)
                {
                    Plugin.Error($"Cannot register update method {method.ToLogName()}: invalid delegate");
                    return;
                }

                _updates.Add(new UpdateData(update.IsUnity, update.PauseWaiting, update.PauseRestarting, update.Delay, del));

                Plugin.Info($"Registered update '{method.ToLogName()}' (delay: {update.Delay} ms)");
            }
            else
            {
                Plugin.Error($"Cannot register update method {method.ToLogName()}: invalid overload parameters");
                return;
            }
        }

        private static IEnumerator<float> Handler()
        {
            for (; ; )
            {
                yield return MEC.Timing.WaitForOneFrame;
                yield return MEC.Timing.WaitForOneFrame;

                foreach (var update in _updates)
                {
                    if (!update.IsUnity && update.CanRun())
                        _updatesQueue.Enqueue(update);
                    else if (update.IsUnity)
                    {
                        if (update.PauseWaiting && RoundHelper.State is Enums.RoundState.WaitingForPlayers)
                            continue;
                        else if (update.PauseRestarting && RoundHelper.State is Enums.RoundState.Restarting)
                            continue;
                        else if (!update.CanRun())
                            continue;
                        else
                            update.DoCall();
                    }
                }
            }
        }

        private static TDelegate BuildDelegate<TDelegate>(MethodInfo method, object target) where TDelegate : Delegate
        {
            try
            {
                return method.CreateDelegate(typeof(TDelegate), target) as TDelegate;
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to build delegate invoker for {method.ToLogName()}");
                Plugin.Error(ex);
            }

            return null;
        }
    }
}
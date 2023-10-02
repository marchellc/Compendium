using Compendium.Comparison;
using Compendium.Round;
using Compendium.Threading;

using helpers;
using helpers.Dynamic;
using helpers.CustomReflect;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using UnityEngine;

using Timer = System.Timers.Timer;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Compendium.Update
{
    public static class UpdateHandler
    {
        private static volatile List<UpdateHandlerData> _handlers = new List<UpdateHandlerData>();
        private static volatile Timer _timer;
        private static volatile Thread _thread;

        public static double NextInterval => Time.deltaTime * 1000f;

        static UpdateHandler()
        {
            _timer = new Timer();
            _timer.Interval = NextInterval;
            _timer.Elapsed += OnElapsed;
            _timer.Enabled = true;

            _thread = CreateThread();
            _thread.Start();
        }

        public static void AddData(MethodInfo target, object handle, UpdateHandlerType type = UpdateHandlerType.Thread, bool main = false, int rate = 1)
        {
            if (type is UpdateHandlerType.Thread
                && !TryValidate(target, main))
                return;

            var data = new UpdateHandlerData(target.GetOrCreateInvoker(), type, main, rate, handle);

            _handlers.Add(data);

            Plugin.Debug($"Registered {type} update handler '{target.ToLogName()}' (main: {main}; rate: {rate})");
        }

        public static bool RemoveData(MethodInfo target, object handle = null)
        {
            return _handlers.RemoveAll(d => DynamicMethodCache.GetOriginalMethod(d.Delegate.Method) == target 
                            && NullableObjectComparison.Compare(d.Handle, handle)) > 0;
        }

        public static bool TryGetData(MethodInfo target, object handle, out UpdateHandlerData handlerData)
            => _handlers.TryGetFirst(d => DynamicMethodCache.GetOriginalMethod(d.Delegate.Method) == target
                            && NullableObjectComparison.Compare(d.Handle, handle), out handlerData);

        private static Thread CreateThread()
        {
            var thread = new Thread(async () =>
            {
                while (true)
                {
                    if (!RoundHelper.IsReady)
                        continue;

                    await Task.Delay(UpdateSynchronizer.LastFrameDuration);

                    var copy = Pools.PoolList(_handlers);

                    foreach (var data in copy)
                    {
                        if (data.Delegate is null)
                            continue;

                        if ((DateTime.Now - data.LastExecute).TotalMilliseconds < data.TickRate)
                            continue;

                        data.LastExecute = DateTime.Now;

                        if (data.ExecuteOnMain)
                        {
                            ThreadScheduler.Schedule(data.Delegate, data.Handle);
                        }
                        else
                        {
                            try
                            {
                                data.Delegate(data.Handle, CachedArray.EmptyObject);
                            }
                            catch (Exception ex)
                            {
                                Plugin.Error($"Failed to invoke update handler: {data.Delegate.Method.ToLogName()}");
                                Plugin.Error(ex);
                            }
                        }
                    }

                    copy.ReturnList();
                }
            });

            thread.Priority = ThreadPriority.Highest;
            return thread;
        }

        private static void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _handlers.For((_, data) =>
                {
                    if (!RoundHelper.IsReady)
                        return;

                    if ((DateTime.Now - data.LastExecute).TotalMilliseconds < data.TickRate)
                        return;

                    if (data.Type is UpdateHandlerType.Engine)
                    {
                        try
                        {
                            ThreadScheduler.Schedule(data.Delegate, data.Handle, CachedArray.EmptyObject);
                            data.LastExecute = DateTime.Now;
                        }
                        catch (Exception ex)
                        {
                            Plugin.Error($"Failed to invoke update handler: {data.Delegate.Method.ToLogName()}");
                            Plugin.Error(ex);
                        }
                    }
                });

                _timer.Interval = NextInterval;
            }
            catch { }
        }

        private static bool TryValidate(MethodInfo method, bool main)
        {
            if (main)
                return true;

            try
            {
                var methodCalls = MethodBodyReader.GetMethodCalls(method);
                var shouldIgnore = method.TryGetAttribute<UpdateIgnoreUnityWarningsAttribute>(out _);

                foreach (var call in methodCalls)
                {
                    if (call.DeclaringType.Assembly.FullName.Contains("Assembly-CSharp")
                        || Reflection.HasType<UnityEngine.Object>(call.DeclaringType))
                    {
                        Plugin.Warn($"Detected a Unity Engine reference in an update handler being executed on a separate thread! Consider using the main thread instead.");
                        Plugin.Warn($"Method: {method.ToLogName()}; Operand: {call.ToLogName()}");

                        if (!shouldIgnore)
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to read instructions of method {method.ToLogName()}\n{ex}");
            }

            return true;
        }
    }
}
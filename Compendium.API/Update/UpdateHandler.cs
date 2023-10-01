using Compendium.Comparison;
using Compendium.Round;
using Compendium.Threading;

using helpers;
using helpers.CustomReflect;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using UnityEngine;

using MonoMod.Utils;

using Timer = System.Timers.Timer;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Compendium.Update
{
    public static class UpdateHandler
    {
        private static readonly HashSet<UpdateHandlerData> _handlers = new HashSet<UpdateHandlerData>();
        private static readonly Timer _timer;

        public static double NextInterval => Time.deltaTime * 1000f;

        static UpdateHandler()
        {
            _timer = new Timer();
            _timer.Interval = NextInterval;
            _timer.Elapsed += OnElapsed;
            _timer.Enabled = true;
        }

        public static void AddData(MethodInfo target, object handle, UpdateHandlerType type = UpdateHandlerType.Thread, bool sync = false, bool main = false, int rate = 1)
        {
            UpdateHandlerData data = null;

            if (type is UpdateHandlerType.Thread)
            {
                if (!TryValidate(target, main))
                    return;

                if (main)
                    data = new UpdateHandlerData(target, handle, type, sync, main, rate);
                else
                    data = new UpdateHandlerData(target.CreateDelegate<Action>(), type, sync, main, rate);
            }
            else
            {
                data = new UpdateHandlerData(target.CreateDelegate<Action>(), type, sync, main, rate);
            }

            if (data is null)
                return;

            if (type is UpdateHandlerType.Thread)
            {
                data.Thread = CreateThread(data);
                data.Thread.Start();
            }

            _handlers.Add(data);

            Plugin.Debug($"Registered {type} update handler '{target.ToLogName()}' (sync: {sync}; main: {main}; rate: {rate})");
        }

        public static bool RemoveData(Action del)
        {
            if (TryGetData(del, out var data))
                data.TokenSource?.Cancel();

            return _handlers.RemoveWhere(d => d.Delegate.Method == del.Method && NullableObjectComparison.Compare(del.Target, d.Delegate.Target)) > 0;
        }

        public static bool TryGetData(Action del, out UpdateHandlerData handlerData)
            => _handlers.TryGetFirst(d => d.Delegate.Method == del.Method && NullableObjectComparison.Compare(del.Target, d.Delegate.Target), out handlerData);

        private static Thread CreateThread(UpdateHandlerData data)
        {
            var thread = new Thread(async () =>
            {
                while (true)
                {
                    data?.Token.ThrowIfCancellationRequested();

                    if (data.Delegate is null || !RoundHelper.IsReady)
                        continue;

                    var delay = data.TickRate <= 0 ? 10 : data.TickRate;

                    if (data.SyncTickRate)
                        delay = UpdateSynchronizer.LastFrameDuration;

                    await Task.Delay(delay);

                    if (data.ExecuteOnMain)
                    {
                        ThreadScheduler.Schedule(data.Method, data.Handle);
                    }
                    else
                    {
                        try
                        {
                            data.Delegate();
                        }
                        catch (Exception ex)
                        {
                            Plugin.Error($"Failed to invoke update handler: {data.Delegate.Method.ToLogName()}");
                            Plugin.Error(ex);
                        }
                    }
                }
            });

            thread.Priority = ThreadPriority.Lowest;
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

                    if (data.Type is UpdateHandlerType.Engine)
                    {
                        try
                        {
                            data.Delegate();
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
                var methodBody = new MethodBodyReader(method);
                var methodCalls = methodBody.GetMethodCalls();
                var shouldIgnore = method.TryGetAttribute<UpdateIgnoreUnityWarningsAttribute>(out _);

                foreach (var call in methodCalls)
                {
                    if (call.DeclaringType.FullName.StartsWith("Unity")
                        || call.DeclaringType.BaseType != null && call.DeclaringType.BaseType.FullName.StartsWith("Unity")
                        || call.DeclaringType.BaseType != null && call.DeclaringType.BaseType.BaseType != null && call.DeclaringType.BaseType.BaseType.FullName.StartsWith("Unity")
                        || call.DeclaringType.BaseType != null && call.DeclaringType.BaseType.Assembly.FullName.Contains("Unity")
                        || call.DeclaringType.BaseType != null && call.DeclaringType.BaseType.Assembly.FullName.Contains("Assembly-CSharp"))
                    {
                        Plugin.Warn($"Detected a Unity Engine reference in an update handler being executed on a separate thread! Consider using the main thread instead.");
                        Plugin.Warn($"Method: {method.ToLogName()}; Operand: {call.ToLogName()}");

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
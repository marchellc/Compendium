using Compendium.Comparison;
using Compendium.Reflect;
using Compendium.Round;
using Compendium.Threading;

using helpers;
using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Compendium.Update
{
    public static class UpdateHandler
    {
        private static readonly HashSet<UpdateHandlerData> _handlers = new HashSet<UpdateHandlerData>();

        static UpdateHandler()
        {
            UpdateSynchronizer.OnUpdate += OnUpdate;
        }

        public static void AddData(Action del, UpdateHandlerType type = UpdateHandlerType.Thread, bool sync = false, bool main = false, int rate = 1)
        {
            var data = new UpdateHandlerData(del, type, sync, main, rate);

            if (type is UpdateHandlerType.Thread)
            {
                CheckForUnityReferences(del, main);

                data.Thread = CreateThread(data);
                data.Thread.Start();
            }

            _handlers.Add(data);
            Plugin.Debug($"Registered {type} update handler '{del.Method.ToLogName()}' (sync: {sync}; main: {main}; rate: {rate})");
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

                    var delay = data.TickRate < 0 ? 10 : data.TickRate;

                    if (data.SyncTickRate)
                        delay = UpdateSynchronizer.LastFrameDuration;

                    await Task.Delay(delay);

                    if (data.ExecuteOnMain)
                    {
                        ThreadScheduler.Schedule(data.Delegate);
                    }
                    else
                    {
                        try
                        {
                            data.Delegate();
                        }
                        catch (Exception ex)
                        {
                            Plugin.Error($"Failed to invoke update handler: {data.Delegate.Method.ToLogName(false)}");
                            Plugin.Error(ex);
                        }
                    }
                }
            });

            thread.Priority = ThreadPriority.Highest;
            return thread;
        }

        private static void OnUpdate()
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
                            Plugin.Error($"Failed to invoke update handler: {data.Delegate.Method.ToLogName(false)}");
                            Plugin.Error(ex);
                        }
                    }
                });
            }
            catch { }
        }

        private static void CheckForUnityReferences(Delegate del, bool isMain)
        {
            try
            {
                if (isMain)
                    return;

                var methodBody = MethodBodyReader.GetInstructions(del.Method);

                methodBody.ForEach(m =>
                {
                    if (m.Operand is MethodBase method)
                    {
                        if (method.DeclaringType.FullName.StartsWith("Unity") 
                        || (method.DeclaringType.BaseType != null && method.DeclaringType.BaseType.FullName.StartsWith("Unity"))
                        || (method.DeclaringType.BaseType != null && method.DeclaringType.BaseType.BaseType != null && method.DeclaringType.BaseType.BaseType.FullName.StartsWith("Unity")))
                        {
                            Plugin.Warn($"Detected a Unity Engine reference in an update handler being executed on a separate thread! Consider using the main thread instead.");
                            Plugin.Warn($"Method: {del.Method.ToLogName()}; Operand: {method.DeclaringType.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name).ToArray())}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to read instructions of method {del.Method.ToLogName(false)}\n{ex}");
            }
        }
    }
}
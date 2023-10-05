using Compendium.Scheduling.Execution;

using helpers;

using Microsoft.Extensions.Logging;

using System;
using System.Reflection;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compendium.Logging
{
    public class Logger : DisposableBase, ILogger
    {
        private static MethodInfo _method;
        private string _sourceName;
        private object[] _args = new object[2];

        public Logger(string source)
        {
            _sourceName = source;
            _method = Reflection.Method<ServerConsole>("AddLog");
        }

        public IDisposable BeginScope<TState>(TState state)
            => this;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _args[0] = $"[{logLevel}] [{_sourceName} ({eventId.Name} / {eventId.Id})] {formatter(state, exception)}{(exception != null ? $"\n{exception}" : "")}";
            _args[1] = exception != null ? ConsoleColor.Red : ConsoleColor.Magenta;

            ExecutionScheduler.Schedule(_method, null, _args, null, null, false, ExecutionThread.Unity);
        }
    }
}
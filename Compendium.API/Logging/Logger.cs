using helpers;

using Microsoft.Extensions.Logging;

using System;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Compendium.Logging
{
    public class Logger : DisposableBase, ILogger
    {
        public Logger(string source) { }

        public IDisposable BeginScope<TState>(TState state)
            => this;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}
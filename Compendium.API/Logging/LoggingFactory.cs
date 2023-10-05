using Microsoft.Extensions.Logging;

namespace Compendium.Logging
{
    public class LoggingProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
            => new Logger(categoryName);

        public void Dispose() { }
    }

    public class LoggingFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }

        public ILogger CreateLogger(string categoryName)
            => new Logger(categoryName);
    }
}
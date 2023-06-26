using helpers;
using helpers.Logging;

namespace Compendium.Logging
{
    public class LoggingProxy : LoggerBase
    {
        public override void Log(LogBuilder log)
        {
            ServerConsole.AddLog(log.Build());
        }
    }
}
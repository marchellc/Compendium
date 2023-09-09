using helpers;
using helpers.Logging;
using helpers.Verify;

using System;

namespace Compendium.Logging
{
    public class LoggingProxy : LoggerBase
    {
        public override void Log(LogBuilder log)
        {
            var str = log.Build();

            if (!VerifyUtils.VerifyString(str))
                return;

            ServerConsole.AddLog(str, GetColor(str));
            UnityEngine.Debug.Log(str);
        }

        private static ConsoleColor GetColor(string log)
        {
            if (log.Contains("INFO"))
                return ConsoleColor.Green;
            else if (log.Contains("ERROR"))
                return ConsoleColor.Red;
            else if (log.Contains("WARN"))
                return ConsoleColor.Yellow;
            else if (log.Contains("DEBUG"))
                return ConsoleColor.Cyan;

            return ConsoleColor.Magenta;
        }
    }
}
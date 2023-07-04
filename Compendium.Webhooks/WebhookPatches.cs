using helpers.Patching;

using System;

namespace Compendium.Webhooks
{
    public static class WebhookPatches
    {
        public static readonly PatchInfo ConsoleLogPatch = new PatchInfo(
            new PatchTarget(typeof(ServerConsole), nameof(ServerConsole.AddLog)),
            new PatchTarget(typeof(WebhookPatches), nameof(WebhookPatches.ConsolePrefix)), PatchType.Prefix, "Console Log Patch");

        public static readonly PatchInfo ServerLogPatch = new PatchInfo(
            new PatchTarget(typeof(ServerLogs), nameof(ServerLogs.AddLog)),
            new PatchTarget(typeof(WebhookPatches), nameof(WebhookPatches.ServerPrefix)), PatchType.Prefix, "Server Log Patch");

        public static bool ConsolePrefix(string q, ConsoleColor color = ConsoleColor.Gray)
        {
            foreach (var webhook in WebhookConfig.Webhooks)
            {
                if (webhook.Type is WebhookLog.Console)
                {
                    webhook.Send($"**[{DateTime.Now.ToString("G")}]** `{q}`");
                }
            }

            ServerConsole.PrintOnOutputs(q, color);
            ServerConsole.PrintFormattedString(q, color);

            return false;
        }

        public static bool ServerPrefix(ServerLogs.Modules module, string msg, ServerLogs.ServerLogType type, bool init = false)
        {
            var time = TimeBehaviour.Rfc3339Time();

            foreach (var webhook in WebhookConfig.Webhooks)
            {
                if (webhook.Type is WebhookLog.Server)
                {
                    webhook.Send($"**[{time}]** <{module} : {type}> `{msg}`");
                }
            }

            var lockObject = ServerLogs.LockObject;

            lock (lockObject)
                ServerLogs.Queue.Enqueue(new ServerLogs.ServerLog(msg, ServerLogs.Txt[(int)type], ServerLogs.Modulestxt[(int)module], time));

            if (init)
                return false;

            ServerLogs._state = ServerLogs.LoggingState.Write;
            return false;
        }
    }
}

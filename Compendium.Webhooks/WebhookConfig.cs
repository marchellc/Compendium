using Compendium.Features;

using helpers.Configuration.Ini;
using System;
using System.Collections.Generic;

namespace Compendium.Webhooks
{
    public static class WebhookConfig
    {
        private static readonly List<WebhookData> _webhooks = new List<WebhookData>();

        public static IReadOnlyList<WebhookData> Webhooks => _webhooks;

        [IniConfig("Webhooks", null, "A list of webhooks.")]
        public static Dictionary<WebhookLog, List<WebhookConfigData>> WebhookList { get; set; } = new Dictionary<WebhookLog, List<WebhookConfigData>>()
        {
            [WebhookLog.Console] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.Server] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.Report] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.CheaterReport] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.BanPrivate] = new List<WebhookConfigData>() { new WebhookConfigData() },
            [WebhookLog.BanPublic] = new List<WebhookConfigData>() { new WebhookConfigData() }
        };

        [IniConfig("Private Bans Include IP", null, "Whether or not to show user's IP address in a private ban log.")]
        public static bool PrivateBansIncludeIp { get; set; } = true;

        [IniConfig("Reports Include IP", null, "Whether or not to show user's IP address in reports.")]
        public static bool ReportsIncludeIp { get; set; } = true;

        [IniConfig("Cheater Reports Include IP", null, "Whether or not to show user's IP address in cheater reports.")]
        public static bool CheaterReportsIncludeIp { get; set; } = true;

        [IniConfig("Send Time", null, "The amount of miliseconds between each queue pull.")]
        public static int SendTime { get; set; } = 500;

        public static void Reload()
        {
            _webhooks.Clear();

            foreach (var pair in WebhookList)
            {
                foreach (var hook in pair.Value)
                {
                    if (!string.IsNullOrWhiteSpace(hook.Url) && hook.Url != "empty")
                        _webhooks.Add(new WebhookData(pair.Key, hook.Url, hook.Content));
                    else
                        FLog.Warn($"Invalid webhook URL: {hook.Url} ({pair.Key})");
                }
            }

            FLog.Info($"Loaded {_webhooks.Count} webhooks.");
        }
    }
}
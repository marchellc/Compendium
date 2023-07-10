using Compendium.Features;

using helpers.Configuration.Ini;

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
        public static int SendTime { get; set; } = 300;

        public static void Reload()
        {
            _webhooks.Clear();

            foreach (var pair in WebhookList)
            {
                foreach (var hook in pair.Value)
                {
                    if (!string.IsNullOrWhiteSpace(hook.Token) && hook.Token != "empty")
                        _webhooks.Add(new WebhookData(pair.Key, hook.Id, hook.MessageId, hook.Token, hook.Content, hook.Username, hook.UserAvatarUrl, hook.AvatarUrl));
                    else
                        FLog.Warn($"Invalid webhook token: {hook.Token} ({pair.Key})");
                }
            }

            FLog.Info($"Loaded {_webhooks.Count} webhooks.");
        }
    }
}
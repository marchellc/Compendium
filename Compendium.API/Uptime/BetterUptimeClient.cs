using Compendium.Events;
using Compendium.Http;

namespace Compendium.Uptime
{
    public static class BetterUptimeClient
    {
        private static string Url => Plugin.Config.BetterUptimeSettings.BetterUptimeUrl;

        [UpdateEvent(TickRate = 25000)]
        private static void OnUpdate()
        {
            if (!string.IsNullOrWhiteSpace(Url) && Url != "none" && Url != "empty")
                HttpDispatch.Post(Url, null, null);
        }
    }
}
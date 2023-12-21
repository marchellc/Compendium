using System.ComponentModel;

namespace Compendium.Settings
{
    public class GuardSettings
    {
        [Description("Steam API key.")]
        public string SteamClientKey { get; set; } = "none";

        [Description("IP Hub API key.")]
        public string VpnClientKey { get; set; } = "none";

        [Description("Use strict mode for VPN filtering.")]
        public bool VpnStrictMode { get; set; } = true;

        [Description("Whether or not to enable pre-authentification counters to enable users to reset their VPN status.")]
        public bool PreAuthCounter { get; set; } = true;
    }
}
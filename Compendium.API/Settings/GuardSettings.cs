using System.ComponentModel;

namespace Compendium.Settings
{
    public class GuardSettings
    {
        [Description("Steam API key.")]
        public string SteamClientKey { get; set; } = "none";

        [Description("IP Hub API key.")]
        public string VpnClientKey { get; set; } = "none";

        [Description("The message to display to kicked users.")]
        public string VpnKickMessage { get; set; } = "Your IP was flagged as a VPN or a proxy network!";

        [Description("Use strict mode for VPN filtering.")]
        public bool VpnStrictMode { get; set; }
    }
}
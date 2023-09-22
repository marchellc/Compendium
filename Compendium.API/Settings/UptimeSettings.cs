using System.ComponentModel;

namespace Compendium.Settings
{
    public class UptimeSettings
    {
        [Description("The URL to a Better Uptime heartbeat service.")]
        public string BetterUptimeUrl { get; set; } = "none";

        [Description("Heartbeat interval, in milliseconds.")]
        public int Interval { get; set; } = 5000;
    }
}
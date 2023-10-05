using Compendium.Constants;

using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class ApiSettings
    {
        [Description("Whetehr or not to reload configs on round restart.")]
        public bool ReloadOnRestart { get; set; } = true;

        [Description("Whether or not to consider everyone with access to the Remote Admin as a staff member. This may be crucial for some features.")]
        public bool ConsiderRemoteAdminAccessAsStaff { get; set; } = false;

        [Description("An alternative server name to be used by some features.")]
        public string AlternativeServerName { get; set; } = "none";

        [Description("A list of features to use global directories.")]
        public List<string> GlobalDirectories { get; set; } = new List<string>()
        {
            "features",
            "data",
            "config"
        };

        [Description("A list of features to use per-server directories.")]
        public List<string> InstanceDirectories { get; set; } = new List<string>();

        [Description("A list of paired announcementss")]
        public Dictionary<ServerStatic.NextRoundAction, string> ServerActionAnnouncements { get; set; } = new Dictionary<ServerStatic.NextRoundAction, string>()
        {
            [ServerStatic.NextRoundAction.Restart] = Colors.LightGreen($"<b>The server is going to restart {Colors.Red("at the end of the round")}!</b>"),
            [ServerStatic.NextRoundAction.Shutdown] = Colors.LightGreen($"<b>The server is going to shut down {Colors.Red("at the end of the round")}!</b>")
        };

        [Description("Settings for the event system.")]
        public EventSettings EventSettings { get; set; } = new EventSettings();

        [Description("Settings for Compendium's custom audio system.")]
        public AudioSettings AudioSettings { get; set; } = new AudioSettings();

        [Description("Settings for Compendium's HTTP dispatch.")]
        public HttpSettings HttpSettings { get; set; } = new HttpSettings();
    }
}
using Compendium.Colors;

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

        [Description("Whether or not to use a safe exception handler.")]
        public bool UseExceptionHandler { get; set; } = true;

        [Description("An alternative server name to be used by some features.")]
        public string AlternativeServerName { get; set; } = "none";

        [Description("A list of features to use global directories.")]
        public List<string> GlobalDirectories { get; set; } = new List<string>()
        {
            "features",
            "data",
            "config"
        };

        [Description("A list of paired announcementss")]
        public Dictionary<ServerStatic.NextRoundAction, string> ServerActionAnnouncements { get; set; } = new Dictionary<ServerStatic.NextRoundAction, string>()
        {
            [ServerStatic.NextRoundAction.Restart] = $"<b><color={ColorValues.LightGreen}>The server is going to restart <color={ColorValues.Red}>at the end of the round</color>!</color></b>",
            [ServerStatic.NextRoundAction.Shutdown] = $"<b><color={ColorValues.LightGreen}>The server is going to shut down <color={ColorValues.Red}>at the end of the round</color>!</color></b>"
        };

        [Description("Settings for the event system.")]
        public EventSettings EventSettings { get; set; } = new EventSettings();

        [Description("Settings for Compendium's custom audio system.")]
        public AudioSettings AudioSettings { get; set; } = new AudioSettings();

        [Description("Settings for Compendium's HTTP dispatch.")]
        public HttpSettings HttpSettings { get; set; } = new HttpSettings();
    }
}
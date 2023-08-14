using Compendium.Colors;

using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class FeatureSettings
    {
        [Description("A list of disabled features.")]
        public List<string> Disabled { get; set; } = new List<string>();

        [Description("A list of features with enabled debug messages.")]
        public List<string> Debug { get; set; } = new List<string>();

        [Description("A list of paired announcementss")]
        public Dictionary<ServerStatic.NextRoundAction, string> ServerActionAnnouncements { get; set; } = new Dictionary<ServerStatic.NextRoundAction, string>()
        {
            [ServerStatic.NextRoundAction.Restart] = $"<b><color={ColorValues.LightGreen}>The server is going to restart <color={ColorValues.Red}>at the end of the round</color>!</color></b>",
            [ServerStatic.NextRoundAction.Shutdown] = $"<b><color={ColorValues.LightGreen}>The server is going to shut down <color={ColorValues.Red}>at the end of the round</color>!</color></b>"
        };
    }
}
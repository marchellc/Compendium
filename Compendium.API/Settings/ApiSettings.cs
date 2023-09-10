using System.Collections.Generic;
using System.ComponentModel;

namespace Compendium.Settings
{
    public class ApiSettings
    {
        [Description("Whether or not to log kicks.")]
        public bool LogKick { get; set; } = true;

        [Description("Whether or not to log bans.")]
        public bool LogBan { get; set; } = true;

        [Description("Whether or not to retrieve player IP's from their auth tokens.")]
        public bool IpCompatibilityMode { get; set; }

        [Description("Whether or not to patch the connection address getter.")]
        public bool IpCompatibilityModePatch { get; set; }

        [Description("Whetehr or not to reload configs and features on round restart.")]
        public bool ReloadOnRestart { get; set; } = true;

        [Description("A list of features to use global directories.")]
        public List<string> GlobalDirectories { get; set; } = new List<string>()
        {
            "features",
            "data",
            "config"
        };
    }
}
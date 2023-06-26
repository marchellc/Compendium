using System.ComponentModel;

namespace Compendium.Settings
{
    public class LogSettings
    {
        [Description("Whether or not to show debug messages.")]
        public bool ShowDebug { get; set; }
    }
}
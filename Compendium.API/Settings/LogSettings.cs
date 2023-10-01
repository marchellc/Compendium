using System.ComponentModel;

namespace Compendium.Settings
{
    public class LogSettings
    {
        [Description("Whether or not to show debug messages.")]
        public bool ShowDebug { get; set; }

        [Description("Whether or not to use a logging proxy for the helpers library.")]
        public bool UseLoggingProxy { get; set; } 
    }
}
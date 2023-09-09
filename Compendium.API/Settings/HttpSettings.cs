using System.ComponentModel;

namespace Compendium.Settings
{
    public class HttpSettings
    {
        [Description("Whether or not to enable the custom HTTP dispatch system.")]
        public bool IsEnabled { get; set; }

        [Description("The maximum amount of times a HTTP request can be requeued. Set to 0 to disable.")]
        public int MaxRequeueCount { get; set; } = 10;

        [Description("Whether or not to show HTTP debug.")]
        public bool Debug { get; set; }
    }
}
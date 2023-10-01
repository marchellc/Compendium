using System.ComponentModel;

namespace Compendium.Settings
{
    public class HttpSettings
    {
        [Description("The maximum amount of times a HTTP request can be requeued. Set to 0 to disable.")]
        public int MaxRequeueCount { get; set; } = 10;

        [Description("Whether or not to show HTTP debug.")]
        public bool Debug { get; set; }
    }
}
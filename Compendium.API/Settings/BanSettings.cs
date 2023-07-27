using System.ComponentModel;

namespace Compendium.Settings
{
    public class BanSettings
    {
        [Description("Whether or not to use the global ban file.")]
        public bool IsGlobal { get; set; } = true;

        [Description("Whether or not to use the custom global ban system.")]
        public bool UseCustom { get; set; } = true;
    }
}
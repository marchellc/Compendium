using System.ComponentModel;

namespace Compendium.Settings
{
    public class CommandSettings
    {
        [Description("Whether or not to use a case-sensitive check for command names.")]
        public bool IsCaseSensitive { get; set; }
    }
}
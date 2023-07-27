using System.ComponentModel;

namespace Compendium.Settings
{
    public class RuleSettings
    {
        [Description("Whether or not to use the global rule file.")]
        public bool IsGlobal { get; set; } = true;
    }
}
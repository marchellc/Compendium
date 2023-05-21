using Compendium.Settings;

using System.ComponentModel;

namespace Compendium
{
    public class Config
    {
        [Description("General log settings.")]
        public LogSettings LogSettings { get; set; } = new LogSettings();

        [Description("General feature settings.")]
        public FeatureSettings FeatureSettings { get; set; } = new FeatureSettings();
    }
}
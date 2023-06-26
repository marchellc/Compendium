using Compendium.Settings;

using System.ComponentModel;

namespace Compendium
{
    public class Config
    {
        [Description("Whether or not to use a safe exception handler.")]
        public bool UseExceptionHandler { get; set; } = true;

        [Description("Whether or not to use a logging proxy for the helpers library.")]
        public bool UseLoggingProxy { get; set; } = true;

        [Description("General log settings.")]
        public LogSettings LogSettings { get; set; } = new LogSettings();

        [Description("General feature settings.")]
        public FeatureSettings FeatureSettings { get; set; } = new FeatureSettings();

        [Description("General staff settings.")]
        public StaffSettings StaffSettings { get; set; } = new StaffSettings();
    }
}
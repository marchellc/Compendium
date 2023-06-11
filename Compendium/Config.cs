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

        [Description("General staff settings.")]
        public StaffSettings StaffSettings { get; set; } = new StaffSettings();

        [Description("General translation settings.")]
        public TranslationSettings TranslationSettings { get; set; } = new TranslationSettings();

        [Description("General voice chat settings.")]
        public VoiceSettings VoiceSettings { get; set; } = new VoiceSettings();
    }
}
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

        [Description("General API settings.")]
        public ApiSettings ApiSetttings { get; set; } = new ApiSettings();

        [Description("Settings for Compendium's custom voice chat.")]
        public VoiceSettings VoiceSettings { get; set; } = new VoiceSettings();

        [Description("Settings for Compendium's warn system.")]
        public WarnSettings WarnSettings { get; set; } = new WarnSettings();

        [Description("Settings for the server guard client.")]
        public GuardSettings GuardSettings { get; set; } = new GuardSettings();
    }
}
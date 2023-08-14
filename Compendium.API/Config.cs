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

        [Description("General API settings.")]
        public ApiSettings ApiSetttings { get; set; } = new ApiSettings();

        [Description("Settings for Compendium's custom ban system.")]
        public BanSettings BanSettings { get; set; } = new BanSettings();

        [Description("Settings for Compendium's custom rule system.")]
        public RuleSettings RuleSettings { get; set; } = new RuleSettings();

        [Description("Settings for Compendium's HTTP dispatch.")]
        public HttpSettings HttpSettings { get; set; } = new HttpSettings();

        [Description("Settings for Compendium's command system.")]
        public CommandSettings CommandSettings { get; set; } = new CommandSettings();

        [Description("Settings for Compendium's custom voice chat.")]
        public VoiceSettings VoiceSettings { get; set; } = new VoiceSettings();
    }
}
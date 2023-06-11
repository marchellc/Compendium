using System.ComponentModel;

namespace Compendium.Settings
{
    public class TranslationSettings
    {
        [Description("Sets the language to be used for translations.")]
        public string Language { get; set; } = "english";
    }
}
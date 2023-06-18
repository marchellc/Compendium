using helpers.Translations;
using System.IO;

namespace Compendium.Translations
{
    public static class Translation
    {
        public static string Path => $"{Plugin.Handler.PluginDirectoryPath}/Translations";

        public static void RegisterAll()
        {
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);

            Translator.Set(Path, Plugin.Config.TranslationSettings.Language, "default");

            Translator.Load();
        }
    }
}
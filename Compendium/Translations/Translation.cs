using helpers.Translations;

namespace Compendium.Translations
{
    public static class Translation
    {
        public static void RegisterAll()
        {
            Translator.Set($"{Plugin.Handler.PluginDirectoryPath}/Translations", Plugin.Config.TranslationSettings.Language, "default");

            Translator.Add("voice.notify", "Changed voice channel to $voiceChannel", "The notification to display when the player switches a voice channel.")
                .WithParameter("voiceChannel", "Channel", "The voice channel the user switched to.");

            Translator.Add("staff.helper.notify.add", "You received a server role: $role", "The notification a staff member receives once they receive a role.")
                .WithParameter("role", "Name", "The role's key.");

            Translator.Add("staff.helper.notify.remove", "Your server role was removed.", "The notification a staff member receives once their role is removed.");

            Translator.Load();
        }
    }
}
using helpers.Translations;

namespace Compendium.Translations
{
    public static class TranslationExtensions
    {
        public static ITranslationEntry WithPlayerParameters(this ITranslationEntry entry, string playerVarName)
        {
            entry.WithParameter($"{playerVarName}.name", "String", $"Gets the {playerVarName}'s name.");
            entry.WithParameter($"{playerVarName}.id", "String", $"Gets the {playerVarName}'s user ID.");
            entry.WithParameter($"{playerVarName}.role", "String", $"Gets the {playerVarName}'s role.");

            return entry;
        }
    }
}
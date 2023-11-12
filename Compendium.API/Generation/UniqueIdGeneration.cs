using Compendium.IO.Saving;

using helpers.Attributes;
using helpers.Random;

using System.Collections.Generic;

namespace Compendium.Generation
{
    public static class UniqueIdGeneration
    {
        private static readonly List<string> _generated = new List<string>();
        private static SaveFile<UniqueIdSaveFile> _generationStorage;

        public static IReadOnlyList<string> Generated => _generated;

        public static bool IsPreviouslyGenerated(string id)
            => _generationStorage.Data.IDs.Contains(id);

        public static string Generate(int length = 10)
        {
            var newId = RandomGeneration.Default.GetReadableString(length).TrimEnd('=');

            while (IsPreviouslyGenerated(newId))
                newId = RandomGeneration.Default.GetReadableString(length).TrimEnd('=');

            _generationStorage.Data.IDs.Add(newId);
            _generationStorage.Save();

            return newId;
        }

        [Load]
        private static void Initialize()
        {
            _generationStorage = new SaveFile<UniqueIdSaveFile>($"{Directories.ThisData}/SavedGenerations");
        }

        [Unload]
        private static void Unload()
        {
            _generationStorage.Save();
        }
    }
}

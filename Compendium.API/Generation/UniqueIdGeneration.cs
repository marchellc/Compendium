using helpers.Attributes;
using helpers.IO.Storage;
using helpers.Random;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Generation
{
    public static class UniqueIdGeneration
    {
        private static readonly List<string> _generated = new List<string>();
        private static IStorageBase _generationStorage;

        public static IReadOnlyList<string> Generated => _generated;

        public static bool IsPreviouslyGenerated(string id)
            => _generationStorage.Contains(id);

        public static string Generate(int length = 10)
        {
            var newId = RandomGeneration.Default.GetReadableString(length).TrimEnd('=');

            while (IsPreviouslyGenerated(newId))
                newId = RandomGeneration.Default.GetReadableString(length).TrimEnd('=');

            _generationStorage.Add(newId);
            return newId;
        }

        [Load]
        private static void Initialize()
        {
            _generationStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGenerations");
            _generationStorage.Load();
        }

        [Unload]
        private static void Unload()
        {
            _generationStorage.Save();
        }
    }
}

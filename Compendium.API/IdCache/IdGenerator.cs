using helpers.Attributes;
using helpers.IO.Storage;
using helpers.Random;

using System.Collections.Generic;

namespace Compendium.IdCache
{
    public static class IdGenerator
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

            Plugin.Debug($"Generated unique ID: {newId}");         
            return newId;
        }

        [Load]
        private static void Initialize()
        {
            _generationStorage = new SingleFileStorage<string>($"{Directories.ThisData}/SavedGenerations");
            _generationStorage.Load();

            Plugin.Info($"ID Generator initialized.");
        }

        [Reload]
        private static void Reload()
        {
            _generationStorage.Reload();
            Plugin.Info("ID Generator reloaded.");
        }

        [Unload]
        private static void Unload()
        {
            _generationStorage.Save();
            Plugin.Info("ID Generator unloaded.");
        }
    }
}
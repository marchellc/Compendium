using BetterCommands;

using helpers.Attributes;
using helpers.Extensions;
using helpers.IO.Binary;
using helpers.Random;
using helpers;

using System.Collections.Generic;
using System.IO;
using System.Text;

using Utils.NonAllocLINQ;

namespace Compendium.Sounds
{
    public static class AudioStore
    {
        private static BinaryImage _manifestImage;

        private static Dictionary<string, string> _manifest;
        private static Dictionary<string, byte[]> _preloaded;

        public static string DirectoryPath { get; } = $"{Directories.ThisData}/AudioFiles";
        public static string ManifestFilePath { get; } = $"{DirectoryPath}/SavedManifest";

        public static IReadOnlyDictionary<string, string> Manifest => _manifest;

        public static bool TryGet(string id, out byte[] oggBytes)
        {
            if (_preloaded.TryGetValue(id, out oggBytes))
                return true;

            if (Manifest.TryGetValue(id, out var filePath))
            {
                if (File.Exists(filePath))
                {
                    oggBytes = File.ReadAllBytes(filePath);
                    return true;
                }
            }

            oggBytes = null;
            return false;
        }

        public static void Save(string id, byte[] oggBytes)
        {
            if (Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains(id) || Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains("*"))
            {
                _preloaded[id] = oggBytes;
                Plugin.Info($"Added audio '{id}' to preloaded files.");
            }

            if (_manifest.TryGetValue(id, out var filePath))
            {
                if (_preloaded.ContainsKey(id))
                    _preloaded[id] = oggBytes;

                File.WriteAllBytes(filePath, oggBytes);

                Plugin.Info($"Overwritten audio '{id}' in the manifest ({oggBytes.Length})");
            }
            else
            {
                filePath = $"{DirectoryPath}/{RandomGeneration.Default.GetReadableString(20).RemovePathUnsafe().Replace("/", "")}";

                File.WriteAllBytes(filePath, oggBytes);

                _manifest[id] = filePath;
                Save();

                Plugin.Info($"Saved audio '{id}' to the manifest ({oggBytes.Length} bytes).");
            }
        }

        [Load]
        [Reload]
        public static void Load()
        {
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            if (_manifestImage != null)
            {
                _manifestImage.Load();

                if (!_manifestImage.TryGetFirst(out _manifest))
                    Save();

                Plugin.Info($"Audio storage reloaded.");
                ReloadManifestMan();
                return;
            }

            _manifest = new Dictionary<string, string>();
            _preloaded = new Dictionary<string, byte[]>();

            _manifestImage = new BinaryImage(ManifestFilePath);
            _manifestImage.Load();

            if (!_manifestImage.TryGetFirst(out _manifest))
                Save();

            ReloadManifestMan();

            Plugin.Info($"Audio storage loaded.");
        }

        private static void ReloadManifestMan()
        {
            Plugin.Info("Reloading the manifest ..");

            if (_manifest is null)
                _manifest = new Dictionary<string, string>();

            if (!_manifestImage.TryGetFirst(out _manifest))
                Save();

            foreach (var file in Directory.GetFiles(DirectoryPath))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName is "SavedManifest" || fileName is "ffmpeg" || fileName is "ffprobe")
                    continue;

                if (!_manifest.TryGetKey(Path.GetFullPath(file), out var key))
                    _manifest.Add(key = Path.GetFileNameWithoutExtension(file), Path.GetFullPath(file));
            }

            var removeList = new List<string>();

            _manifest.ForEach(pair =>
            {
                if (!File.Exists(pair.Value))
                    removeList.Add(pair.Key);
            });

            removeList.ForEach(id => _manifest.Remove(id));

            _preloaded.Clear();

            _manifest.ForEach(pair =>
            {
                if (Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains(pair.Key) || Plugin.Config.ApiSetttings.AudioSettings.PreloadIds.Contains("*"))
                {
                    if (File.Exists(pair.Value))
                    {
                        _preloaded[pair.Key] = File.ReadAllBytes(pair.Value);
                        Plugin.Info($"Preloaded audio '{pair.Key}' from file '{Path.GetFileName(pair.Value)}'");
                    }
                    else
                        Plugin.Warn($"Failed to preload '{pair.Key}': file '{Path.GetFileName(pair.Value)}' does not exist!");
                }
            });

            Save();
        }

        [Unload]
        public static void Unload()
        {
            Save();

            _manifestImage.Clear();
            _manifestImage = null;
        }

        [Command("armanifest", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Reloads the audio storage manifest.")]
        private static string ReloadManifest(ReferenceHub sender)
        {
            ReloadManifestMan();
            return "Reloaded the storage manifest.";
        }

        [Command("listmanifest", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Views all files in the manifest.")]
        private static string ListManifest(ReferenceHub sender)
        {
            if (!_manifest.Any())
                return "There are no files in the manifest.";

            var sb = new StringBuilder();

            sb.AppendLine($"Showing {_manifest.Count} files in the manifest:");

            _manifest.For((i, pair) => sb.AppendLine($"[{i + 1}] {pair.Key} ({Path.GetFileName(pair.Value)})"));

            return sb.ToString();
        }

        [Command("clearmanifest", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Clears the manifest.")]
        private static string ClearManifest(ReferenceHub sender, bool deleteFiles)
        {
            if (deleteFiles)
            {
                foreach (var file in Directory.GetFiles(DirectoryPath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    if (fileName is "SavedManifest" || fileName is "ffmpeg" || fileName is "ffprobe")
                        continue;

                    File.Delete(file);

                    sender.Message($"Deleted file: {fileName}", true);
                }
            }

            _manifest.Clear();
            _preloaded.Clear();

            Save();
            return "Cleared the audio manifest.";
        }

        [Command("deletemanifest", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Removes an audio file from the manifest.")]
        private static string DeleteManifest(ReferenceHub sender, string id, bool deleteFile)
        {
            if (_manifest.TryGetValue(id, out var filePath) && deleteFile)
                File.Delete(filePath);

            _manifest.Remove(id);
            _preloaded.Remove(id);

            Save();
            return $"Removed '{id}' from the manifest.";
        }

        public static void Save()
        {
            if (_manifest is null)
                _manifest = new Dictionary<string, string>();

            if (_manifestImage != null)
            {
                _manifestImage.Clear();
                _manifestImage.Add(_manifest);
                _manifestImage.Save();
            }
        }
    }
}
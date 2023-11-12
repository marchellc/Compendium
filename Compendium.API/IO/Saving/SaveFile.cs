using System;
using System.IO;

namespace Compendium.IO.Saving
{
    public class SaveFile<TData> where TData : SaveData, new()
    {
        public DateTime SaveTime { get; private set; }

        public Watcher.Watcher Watcher { get; private set; }

        public TData Data { get; private set; }

        public string Path { get; }

        public bool IsUsingWatcher
        {
            get => Watcher != null;
            set
            {
                if (Watcher is null && !value)
                    return;

                if (Watcher != null && value)
                    return;

                if (value)
                {
                    Watcher = new Watcher.Watcher(Path);
                    Watcher.OnFileChanged += Load;

                    Plugin.Debug($"Enabled file watcher for file '{Path}'");
                }
                else
                {
                    if (Watcher != null)
                    {
                        Watcher.OnFileChanged -= Load;
                        Watcher = null;
                    }

                    Plugin.Debug($"Disabled file watcher for file '{Path}'");
                }
            }
        }

        public SaveFile(string path, bool useWatcher = true)
        {
            Path = path;

            IsUsingWatcher = useWatcher;

            Load();
        }

        public void Load()
        {
            if (Watcher != null && Watcher.IsRecent)
                return;

            Plugin.Debug($"Loading save file '{Path}'");

            if (!File.Exists(Path))
            {
                Save();
                return;
            }

            Data ??= new TData();

            try
            {
                if (Data.IsBinary)
                {
                    using (var fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var br = new BinaryReader(fs))
                        Data.Read(br);
                }
                else
                {
                    using (var fs = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                        Data.Read(sr);
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to load save file '{Path}' - the save data handler failed with an exception:\n{ex}");
            }

            SaveTime = DateTime.Now;
        }

        public void Save()
        {
            Plugin.Debug($"Saving save file '{Path}'");

            Data ??= new TData();

            Watcher!.IsRecent = true;

            try
            {
                if (!File.Exists(Path))
                    File.Create(Path).Close();

                if (Data.IsBinary)
                {
                    using (var fs = new FileStream(Path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var bw = new BinaryWriter(fs))
                        Data.Write(bw);
                }
                else
                {
                    using (var fs = new FileStream(Path, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite))
                    using (var sw = new StreamWriter(fs))
                        Data.Write(sw);
                }
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to save save file '{Path}' - the save data handler failed with an exception:\n{ex}");
            }

            SaveTime = DateTime.Now;
        }
    }
}
using System;
using System.IO;

namespace Compendium.IO.Watcher
{
    public class Watcher
    {
        private string _file;
        private bool _recent;

        public event Action OnFileChanged;

        public bool IsRecent
        {
            get => _recent;
            set
            {
                if (_recent == value || value is false)
                    return;

                _recent = value;

                Calls.Delay(1f, () =>
                {
                    _recent = false;
                });
            }
        }

        public Watcher(string path)
        {
            _file = Path.GetFullPath(path);

            var watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                NotifyFilter = NotifyFilters.LastWrite
            };

            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            Plugin.Debug($"Enabled file watcher for file '{_file}'");
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.FullPath != _file)
                return;

            if (_recent)
                return;

            IsRecent = true;

            OnFileChanged?.Invoke();
        }
    }
}
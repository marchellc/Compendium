namespace Compendium.Features
{
    public interface IFeature
    {
        string Name { get; }

        bool IsDisabled { get; }
        bool IsRunning { get; }

        void Load();
        void Unload();
        void Reload();
        void Update();
        void Disable();
        void Enable();
    }
}
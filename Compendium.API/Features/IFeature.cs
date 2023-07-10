namespace Compendium.Features
{
    public interface IFeature
    {
        string Name { get; }

        bool IsPatch { get; }
        bool IsEnabled { get; }

        void Load();
        void Unload();
        void Reload();

        void Restart();

        void OnWaiting();

        void CallUpdate();
    }
}
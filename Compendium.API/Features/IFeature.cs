namespace Compendium.Features
{
    public interface IFeature
    {
        string Name { get; }

        void Load();
        void Unload();
        void Reload();
    }
}
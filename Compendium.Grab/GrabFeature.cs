using Compendium.Features;

namespace Compendium.Grab
{
    public class GrabFeature : IFeature
    {
        public string Name => "Grab";

        public void Load()
            => GrabHandler.Load();

        public void Reload()
            => GrabHandler.Reload();

        public void Unload()
            => GrabHandler.Unload();
    }
}
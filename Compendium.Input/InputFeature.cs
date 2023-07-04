using Compendium.Features;

namespace Compendium.Input
{
    public class InputFeature : IFeature
    {
        public string Name => "Input";

        public void Load() => InputHandler.Load();
        public void Reload() => InputHandler.Reload();
        public void Unload() => InputHandler.Unload();
    }
}
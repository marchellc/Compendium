using Compendium.Features;

namespace Compendium.Input
{
    public class InputFeature : ConfigFeatureBase
    {
        public override string Name => "Input";

        public override void Load()
        {
            base.Load();
            InputHandler.Load();
        }

        public override void Reload()
        {
            base.Reload();
            InputHandler.Reload();
        }

        public override void Unload()
        {
            base.Unload();
            InputHandler.Unload();
        }
    }
}
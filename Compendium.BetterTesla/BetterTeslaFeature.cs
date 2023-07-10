using Compendium.Features;

namespace Compendium.BetterTesla
{
    public class BetterTeslaFeature : ConfigFeatureBase
    {
        public override string Name => "Better Tesla";
        public override bool IsPatch => true;

        public override void Load()
        {
            base.Load();
            BetterTeslaLogic.IsEnabled = true;
        }
    }
}
using Compendium.Features;

namespace Compendium.Gameplay
{
    public class GameplayFeature : ConfigFeatureBase
    {
        public override bool IsPatch => true;
        public override string Name => "Gameplay";
    }
}
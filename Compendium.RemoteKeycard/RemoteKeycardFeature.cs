using Compendium.Features;

namespace Compendium.RemoteKeycard
{
    public class RemoteKeycardFeature : ConfigFeatureBase
    {
        public override string Name => "Remote Keycard";
        public override bool IsPatch => true;
    }
}
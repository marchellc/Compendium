using Compendium.Features;

namespace Compendium.Voice
{
    public class VoiceFeature : ConfigFeatureBase
    {
        public override string Name => "Voice";
        public override bool IsPatch => true;

        public override void Load()
        {
            base.Load();
            VoiceController.Load();
            VoiceUtils.Load();
        }

        public override void Unload()
        {
            base.Unload();
            VoiceController.Unload();
        }
    }
}
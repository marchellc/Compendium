using Compendium.Features;

namespace Compendium.Scp914
{
    public class Scp914Feature : ConfigFeatureBase
    {
        public override string Name => "SCP-914";

        public override void Load()
        {
            base.Load();
            Scp914Logic.Load();
        }

        public void Unload()
        {
            Scp914Logic.Unload();
            base.Unload();
        }
    }
}
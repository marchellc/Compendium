using Compendium.Features;
using Compendium.Fixes.RoleSpawn;

namespace Compendium.Fixes
{
    public class FixFeature : ConfigFeatureBase
    {
        public override string Name => "Fix";
        public override bool IsPatch => false;

        public override void Load()
        {
            base.Load();
            RoleSpawnHandler.Load();
        }

        public void Unload()
        {
            base.Unload();
            RoleSpawnHandler.Unload();
        }
    }
}
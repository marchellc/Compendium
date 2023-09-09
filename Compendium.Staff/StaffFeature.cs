using Compendium.Features;

namespace Compendium.Staff
{
    public class StaffFeature : ConfigFeatureBase
    {
        public override bool IsPatch => true;
        public override string Name => "Staff";

        public static StaffFeature Singleton { get; set; }

        public override void Load()
        {
            base.Load();
            Singleton = this;
            StaffHandler.Initialize();
        }

        public override void Reload()
        {
            base.Reload();
            StaffHandler.Reload();
        }
    }
}
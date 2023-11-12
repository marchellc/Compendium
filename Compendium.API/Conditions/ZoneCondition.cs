using MapGeneration;

namespace Compendium.Conditions
{
    public class ZoneCondition : Condition
    {
        private FacilityZone _zone;

        public ZoneCondition(FacilityZone zone)
            => _zone = zone;

        public override bool IsMatch(ReferenceHub hub)
            => hub.Zone() == _zone;
    }
}
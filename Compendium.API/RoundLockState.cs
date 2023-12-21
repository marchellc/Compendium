using Footprinting;

namespace Compendium
{
    public class RoundLockState
    {
        public bool IsActive { get; set; }     
        public Footprint EnabledBy { get; set; }
    }
}
namespace Compendium.ServerGuard.VpnShield
{
    public class VpnShieldData
    {
        public string UniqueId { get; set; }
        public VpnShieldFlags Flags { get; set; } = VpnShieldFlags.Clean;
    }
}
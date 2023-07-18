namespace Compendium.ServerGuard.VpnShield
{
    public class VpnShieldData
    {
        public string UserId { get; set; }
        public string Ip { get; set; } 
        public string Token { get; set; }

        public VpnShieldFlags Flags { get; set; } = VpnShieldFlags.Clean;
    }
}
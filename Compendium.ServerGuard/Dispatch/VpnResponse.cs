using Compendium.ServerGuard.VpnShield;

using System.Text.Json.Serialization;

namespace Compendium.ServerGuard.Dispatch
{
    public class VpnResponse
    {
        [JsonPropertyName("ip")]
        public string Address { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        [JsonPropertyName("isp")]
        public string Provider { get; set; }

        [JsonPropertyName("asn")]
        public int AsnId { get; set; }

        [JsonPropertyName("block")]
        public int BlockId { get; set; }

        public VpnShieldFlags Flags
        {
            get
            {
                if (BlockId == 0)
                    return VpnShieldFlags.Clean;

                if (BlockId == 1)
                    return VpnShieldFlags.Kick;

                if (BlockId == 2 && VpnShieldHandler.VpnStrict)
                    return VpnShieldFlags.Kick;

                return VpnShieldFlags.Clean;
            }
        }
    }
}
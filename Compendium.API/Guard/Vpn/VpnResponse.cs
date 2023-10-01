using System.Text.Json.Serialization;

namespace Compendium.Guard.Vpn
{
    public class VpnResponse
    {
        [JsonPropertyName("ip")]
        public string Ip { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        [JsonPropertyName("isp")]
        public string Provider { get; set; }

        [JsonPropertyName("asn")]
        public int Asn { get; set; }

        [JsonPropertyName("block")]
        public int BlockLevel { get; set; }
    }
}
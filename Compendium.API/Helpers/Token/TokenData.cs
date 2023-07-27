using System;
using System.Text.Json.Serialization;

namespace Compendium.Helpers.Token
{
    public class TokenData
    {
        [JsonPropertyName("target_user_id")] public string UserId { get; set; }
        [JsonPropertyName("target_nickname")] public string Nickname { get; set; }
        [JsonPropertyName("target_ip")] public string Ip { get; set; }

        [JsonPropertyName("token_eh_id")] public string EhId { get; set; }
        [JsonPropertyName("token_issuer_server_id")] public string ServerId { get; set; }
        [JsonPropertyName("token_usage_type")] public string UsageType { get; set; }
        [JsonPropertyName("token_serial")] public string SerialNumber { get; set; }
        [JsonPropertyName("token_signature")] public string Signature { get; set; }
        [JsonPropertyName("token_public_part")] public string PublicPart { get; set; }

        [JsonPropertyName("token_asn_id")] public int AsnId { get; set; }
        [JsonPropertyName("token_auth_ver")] public int AuthVersion { get; set; }

        [JsonPropertyName("token_issued_at")] public DateTime IssuedAt { get; set; }
        [JsonPropertyName("token_expires_at")] public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("flags_global_ban")] public bool IsGloballyBanned { get; set; }
        [JsonPropertyName("flags_vac_session")] public bool IsVacSession { get; set; }
        [JsonPropertyName("flags_test_signature")] public bool IsTestSignature { get; set; }
        [JsonPropertyName("flags_do_not_track")] public bool IsDoNotTrack { get; set; } 

        [JsonPropertyName("signals_ip_check")] public bool ShouldSkipIpCheck { get; set; }
        [JsonPropertyName("signals_sync_hashed")] public bool ShouldSyncHashed { get; set; }

        [JsonPropertyName("signals_ban_bypass")] public bool CanBypassBans { get; set; }
        [JsonPropertyName("signals_geo_block_bypass")] public bool CanBypassGeoRestrictions { get; set; }
        [JsonPropertyName("signals_whitelist_bypass")] public bool CanBypassWhitelist { get; set; }

        [JsonPropertyName("signals_global_badge")] public bool HasGlobalBadge { get; set; }
    }
}

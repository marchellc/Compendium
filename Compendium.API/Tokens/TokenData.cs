using System;

namespace Compendium.TokenCache
{
    public class TokenData
    {
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public string Ip { get; set; }

        public string EhId { get; set; }
        public string ServerId { get; set; }
        public string UsageType { get; set; }
        public string SerialNumber { get; set; }
        public string Signature { get; set; }
        public string PublicPart { get; set; }

        public int AsnId { get; set; }
        public int AuthVersion { get; set; }

        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public bool IsGloballyBanned { get; set; }
        public bool IsVacSession { get; set; }
        public bool IsTestSignature { get; set; }
        public bool IsDoNotTrack { get; set; } 

        public bool ShouldSkipIpCheck { get; set; }
        public bool ShouldSyncHashed { get; set; }

        public bool CanBypassBans { get; set; }
        public bool CanBypassGeoRestrictions { get; set; }
        public bool CanBypassWhitelist { get; set; }

        public bool HasGlobalBadge { get; set; }
    }
}

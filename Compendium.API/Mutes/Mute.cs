using System;

namespace Compendium.Mutes
{
    public class Mute
    {
        public string Id { get; set; }
        
        public string TargetId { get; set; }
        public string IssuerId { get; set; }

        public string Reason { get; set; }

        public long IssuedAt { get; set; }
        public long ExpiresAt { get; set; }

        public bool IsExpired() => DateTime.Now.Ticks >= ExpiresAt;
    }
}
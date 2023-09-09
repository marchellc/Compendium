using System;

namespace Compendium.Warns
{
    public class WarnData
    {
        public string Issuer { get; set; }
        public string Target { get; set; }
        public string Reason { get; set; }
        public string Id { get; set; }

        public DateTime IssuedAt { get; set; }
    }
}
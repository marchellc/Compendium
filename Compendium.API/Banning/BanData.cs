using System;

namespace Compendium.Banning
{
    public class BanData
    {
        public string IssuedBy { get; set; }
        public string IssuedTo { get; set; }
        public string Reason { get; set; }
        public string Id { get; set; }

        public DateTime IssuedAt { get; set; }
        public DateTime EndsAt { get; set; }
    }
}
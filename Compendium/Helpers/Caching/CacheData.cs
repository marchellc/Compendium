using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compendium.Helpers.Caching
{
    public class CacheData
    {
        public string UniqueId { get; set; } = "null";

        public string Ip { get; set; } = "null";

        public string LastId { get; set; } = "null";
        public string LastName { get; set; } = "null";

        public Dictionary<string, DateTime> AllIds { get; set; } = new Dictionary<string, DateTime>();
        public Dictionary<string, DateTime> AllNames { get; set; } = new Dictionary<string, DateTime>();

        public DateTime LastOnline { get; set; } = DateTime.MinValue;

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb  .AppendLine()
                .AppendLine($"<-- Cached Data -->")
                .AppendLine($"• Username: {LastName}")
                .AppendLine($"• User ID: {LastId}")
                .AppendLine($"• User IP: {Ip}")
                .AppendLine($"• Unique ID: {UniqueId}")
                .AppendLine($"• Last seen: {LastOnline.ToString("F")}");

            if (AllIds.Where(x => x.Key != LastId).Any())
            {
                sb.AppendLine()
                  .AppendLine($"--- Displaying all User IDs of this user ({AllIds.Count}) ---");

                for (int i = 0; i < AllIds.Count; i++)
                {
                    var pair = AllIds.ElementAt(i);
                    sb.AppendLine($"[{i + 1}]: {pair.Key} (changed at: {pair.Value.ToString("F")}");
                }
            }

            if (AllNames.Where(x => x.Key != LastName).Any())
            {
                sb.AppendLine()
                  .AppendLine($"--- Displaying all names of this user ({AllNames.Count}) ---");

                for (int i = 0; i < AllIds.Count; i++)
                {
                    var pair = AllNames.ElementAt(i);
                    sb.AppendLine($"[{i + 1}]: {pair.Key} (changed at: {pair.Value.ToString("F")}");
                }
            }

            return sb.ToString();
        }
    }
}
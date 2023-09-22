using System.Collections.Generic;

namespace Compendium.Webhooks
{
    public class WebhookEvent : WebhookData
    {
        public static string Timestamp => helpers.Time.TimeUtils.LocalTime.ToString("T");

        public IReadOnlyList<WebhookEventLog> AllowedEvents { get; }

        public WebhookEvent(string url, List<WebhookEventLog> allowed) : base(WebhookLog.Event, url, null) 
        {
            AllowedEvents = allowed;
        }

        public void Event(string msg) 
            => Send($"[{Timestamp}] {msg}", null);

        public void Event(Discord.DiscordEmbed embed)
        {
            embed.WithField("🕒 Time", Timestamp);
            Send(null, embed);
        }
    }
}
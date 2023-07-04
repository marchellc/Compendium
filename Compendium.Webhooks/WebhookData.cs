using Compendium.Features;
using Compendium.Webhooks.Discord;

using System;
using System.Collections.Concurrent;

namespace Compendium.Webhooks
{
    public class WebhookData
    {
        public WebhookLog Type { get; }

        public Uri Url { get; }

        public string Content { get; }

        public ConcurrentQueue<DiscordMessage> Queue { get; } = new ConcurrentQueue<DiscordMessage>();

        public DateTime? Next { get; set; }

        public WebhookData(WebhookLog type, string url, string content = null)
        {
            Type = type;
            Content = content;

            if (string.IsNullOrWhiteSpace(Content) || Content is "empty")
            {
                Content = null;
            }
            
            if (Uri.TryCreate(url, UriKind.Absolute, out var result))
            {
                Url = result;
            }
            else
            {
                Url = null;
                FLog.Warn($"Failed to parse URL: {url}");
            }
        }

        public void Send(string content = null, Discord.DiscordEmbed? embed = null)
        {
            if (Url is null)
            {
                FLog.Warn($"Attempted to send message on an invalid webhook!");
                return;
            }

            if (content is null && Content != null)
            {
                content = Content;
            }

            if (content is null && !embed.HasValue)
            {
                FLog.Warn($"Attempted to send an empty message!");
                return;
            }

            var message = new DiscordMessage();

            if (!string.IsNullOrWhiteSpace(content))
            {
                message.WithContent(content);
            }

            if (embed.HasValue)
            {
                message.WithEmbeds(embed.Value);
            }

            Queue.Enqueue(message);
        }
    }
}
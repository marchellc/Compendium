using Compendium.Features;
using Compendium.Webhooks.Discord;

using System;
using System.Collections.Concurrent;

namespace Compendium.Webhooks
{
    public class WebhookData
    {
        public WebhookLog Type { get; }

        public string Content { get; }
        public string Token { get; }

        public string UserName { get; }
        public string UserAvatarUrl { get; }

        public string AvatarUrl { get; }

        public long Id { get; }
        public long MessageId { get; }

        public bool TargetMessageSet { get; set; }
        public bool HasTargetMessage => Type is WebhookLog.ServerStatus;

        public object Lock { get; } = new object();

        public ConcurrentQueue<DiscordWebhookPayload> Queue { get; } = new ConcurrentQueue<DiscordWebhookPayload>();

        public DateTime? Next { get; set; }

        public WebhookData(
            WebhookLog type, 
            
            long id, 
            long msgId, 
            
            string token, 
            
            string content = null, 
            string userName = null, 
            string userAvatarUrl = null, 
            string avatarUrl = null)
        {
            Type = type;

            Content = content;

            Id = id;
            MessageId = msgId;

            Token = token;
            UserName = userName;
            UserAvatarUrl = userAvatarUrl;

            AvatarUrl = avatarUrl;

            TargetMessageSet = MessageId > 0;

            if (string.IsNullOrWhiteSpace(Content) || Content is "empty")
                Content = null;

            if (string.IsNullOrWhiteSpace(UserName) || UserName is "empty")
                UserName = null;

            if (string.IsNullOrWhiteSpace(UserAvatarUrl) || UserAvatarUrl is "empty")
                UserAvatarUrl = null;

            if (string.IsNullOrWhiteSpace(AvatarUrl) || AvatarUrl is "empty")
                AvatarUrl = null;
        }

        public void Delete(long msgId)
            => Queue.Enqueue(new DiscordWebhookPayload(msgId, null, DiscordWebhookPayloadType.Delete, null));

        public void Edit(long msgId, string content = null, Discord.DiscordEmbed? embed = null)
        {
            if (content is null && Content != null)
                content = Content;

            if (content is null && !embed.HasValue)
            {
                FLog.Warn($"Attempted to send an empty message!");
                return;
            }

            var message = new DiscordMessage();

            if (!string.IsNullOrWhiteSpace(content))
                message.WithContent(content);

            if (embed.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(AvatarUrl))
                {
                    if (embed.Value.Author.HasValue)
                        embed.Value.Author.Value.WithIcon(AvatarUrl);
                    else
                        embed.Value.WithAuthor(null, null, AvatarUrl);
                }

                message.WithEmbeds(embed.Value);
            }

            if (!string.IsNullOrWhiteSpace(UserName))
                message.WithUser(UserName, UserAvatarUrl);

            Queue.Enqueue(new DiscordWebhookPayload(msgId, message, DiscordWebhookPayloadType.Edit, null));
        }

        public void Send(string content = null, Discord.DiscordEmbed? embed = null)
        {
            if (content is null && Content != null)
                content = Content;

            if (content is null && !embed.HasValue)
            {
                FLog.Warn($"Attempted to send an empty message!");
                return;
            }

            var message = new DiscordMessage();

            if (!string.IsNullOrWhiteSpace(content))
                message.WithContent(content);

            if (embed.HasValue)
            {
                if (!string.IsNullOrWhiteSpace(AvatarUrl))
                {
                    if (embed.Value.Author.HasValue)
                        embed.Value.Author.Value.WithIcon(AvatarUrl);
                    else
                        embed.Value.WithAuthor(null, null, AvatarUrl);
                }

                message.WithEmbeds(embed.Value);
            }

            if (!string.IsNullOrWhiteSpace(UserName))
                message.WithUser(UserName, UserAvatarUrl);

            Queue.Enqueue(new DiscordWebhookPayload(0, message, DiscordWebhookPayloadType.Post, null));
        }
    }
}
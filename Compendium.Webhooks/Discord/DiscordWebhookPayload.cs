using System;

namespace Compendium.Webhooks.Discord
{
    public struct DiscordWebhookPayload
    {
        public long MessageId;
        public DiscordMessage? Message;
        public DiscordWebhookPayloadType PayloadType;
        public Action<DiscordMessage?> Callback;

        public DiscordWebhookPayload(long msgId, DiscordMessage? message, DiscordWebhookPayloadType type, Action<DiscordMessage?> callback)
        {
            MessageId = msgId;
            Message = message;
            PayloadType = type;
            Callback = callback;
        }
    }
}
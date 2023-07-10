namespace Compendium.Webhooks
{
    public class WebhookConfigData
    {
        public string Content { get; set; } = "empty";
        public string Username { get; set; } = "empty";
        public string UserAvatarUrl { get; set; } = "empty";
        public string AvatarUrl { get; set; } = "empty";
        public string Token { get; set; }

        public long Id { get; set; } = 0;
        public long MessageId { get; set; } = 0;
    }
}
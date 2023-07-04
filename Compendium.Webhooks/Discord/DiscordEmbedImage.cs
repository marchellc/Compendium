using System.Text.Json.Serialization;

namespace Compendium.Webhooks.Discord
{
    public struct DiscordEmbedImage
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("proxy_url")]
        public string ProxyUrl { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        public static DiscordEmbedImage Create(string url, string proxy = null, int? height = null, int? width = null)
            => new DiscordEmbedImage()
            {
                Url = url,
                ProxyUrl = proxy,
                Height = height,
                Width = width
            };
    }
}
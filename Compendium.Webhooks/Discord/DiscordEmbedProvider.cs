using System.Text.Json.Serialization;

namespace Compendium.Webhooks.Discord
{
    public struct DiscordEmbedProvider
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        public static DiscordEmbedProvider Create(string name, string url)
            => new DiscordEmbedProvider() { Name = name, Url = url };
    }
}
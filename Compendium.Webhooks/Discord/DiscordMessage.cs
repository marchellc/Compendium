using helpers;
using helpers.Json;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Compendium.Webhooks.Discord
{
    public struct DiscordMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("tts")]
        public bool IsTextToSpeech { get; set; }

        [JsonPropertyName("embeds")]
        public DiscordEmbed[] Embeds { get; set; }

        [JsonPropertyName("allowed_mentions")]
        public DiscordMessageAllowedMentions? Mentions { get; set; }

        public DiscordMessage WithContent(string content, bool isTts = false)
        {
            Content = content;
            IsTextToSpeech = isTts;
            return this;
        }

        public DiscordMessage WithTextToSpeech(bool isTts = true)
        {
            IsTextToSpeech = isTts;
            return this;
        }

        public DiscordMessage WithEmbeds(params DiscordEmbed[] embeds)
        {
            if (Embeds != null && Embeds.Any())
            {
                var list = new List<DiscordEmbed>(Embeds);

                list.AddRange(embeds);

                Embeds = list.ToArray();
                return this;
            }

            Embeds = embeds;
            return this;
        }

        public DiscordMessage WithMentions(DiscordMessageAllowedMentions? discordMessageAllowedMentions)
        {
            Mentions = discordMessageAllowedMentions;
            return this;
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(Content))
            {
                if (Content.Length >= 1900)
                {
                    Content = Content.Substring(0, 1900) + " ...";
                }
            }

            if (Embeds != null && Embeds.Any())
            {
                foreach (var embed in Embeds)
                {
                    if (!string.IsNullOrWhiteSpace(embed.Description))
                    {
                        if (embed.Description.Length >= 1900)
                        {
                            var desc = embed.Description;
                            desc = desc.Substring(0, 1900) + " ...";
                            embed.WithDescription(desc);
                        }
                    }
                }
            }

            return JsonSerializer.Serialize(this);
        }

        public static DiscordMessage FromJson(string json)
            => JsonHelper.FromJson<DiscordMessage>(json);
    }
}

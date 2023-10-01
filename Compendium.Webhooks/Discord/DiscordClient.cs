using Compendium.Events;
using Compendium.Features;
using Compendium.Logging;

using helpers.Attributes;
using helpers.Json;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Compendium.Webhooks.Discord
{
    public static class DiscordClient
    {
        private static MediaTypeHeaderValue _jsonHeader = MediaTypeHeaderValue.Parse("application/json");
        private static HttpClient _client;
        private static DateTime? _lastCheck;

        [Load]
        public static void Load()
        {
            _client = new HttpClient();
        }

        [Unload]
        public static void Unload()
        {
            _client.Dispose();
            _client = null;
        }

        [UpdateEvent]
        public static void Update()
        {
            if (_lastCheck.HasValue)
            {
                if ((DateTime.Now - _lastCheck.Value).Milliseconds > WebhookHandler.SendTime)
                    _lastCheck = DateTime.Now;
                else
                    return;
            }
            else
            {
                _lastCheck = DateTime.Now;
            }

            foreach (var webhook in WebhookHandler.Webhooks)
            {
                if (webhook.Next.HasValue)
                {
                    if (!(DateTime.Now >= webhook.Next.Value))
                        continue;

                    webhook.Next = null;
                }

                if (webhook.Queue.TryDequeue(out var message))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
                            var httpContent = new MultipartFormDataContent(bound);
                            var jsonContent = new StringContent(JsonSerializer.Serialize(message));

                            jsonContent.Headers.ContentType = _jsonHeader;

                            httpContent.Add(jsonContent, "payload_json");

                            using (var response = await _client.PostAsync(webhook.Url, httpContent))
                            {
                                if (!response.IsSuccessStatusCode)
                                    webhook.Next = DateTime.Now + TimeSpan.FromSeconds(2);
                                else
                                    webhook.Next = null;
                            }
                        }
                        catch (Exception ex)
                        {
                            FLog.Error($"Failed to send payload:\n{ex}", new LogParameter("payload", message.ToJson()), new LogParameter("destination", webhook.Url.ToString()));
                        }
                    });
                }
            }
        }
    }
}
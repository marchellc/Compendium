using Compendium.Features;
using Compendium.Logging;

using helpers;
using helpers.Extensions;
using helpers.Json;
using System;
using System.Collections.Concurrent;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Compendium.Webhooks.Discord
{
    public static class DiscordClient
    {
        private static MediaTypeHeaderValue _jsonHeader = MediaTypeHeaderValue.Parse("application/json");
        private static HttpClient _client;
        private static DateTime? _lastCheck;

        public static void Load()
        {
            _client = new HttpClient();
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", Update);
        }

        public static void Unload()
        {
            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", Update);

            _client.Dispose();
            _client = null;
        }

        private static void Update()
        {
            if (_lastCheck.HasValue)
            {
                if ((DateTime.Now - _lastCheck.Value).Milliseconds > WebhookConfig.SendTime)
                    _lastCheck = DateTime.Now;
                else
                    return;
            }
            else
            {
                _lastCheck = DateTime.Now;
            }

            foreach (var webhook in WebhookConfig.Webhooks)
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
                            var jsonContent = new StringContent(message.ToJson());

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
                            FLog.Error($"Failed to send payload:\n{ex}", new DebugParameter("payload", message.ToJson()), new DebugParameter("destination", webhook.Url.ToString()));
                        }
                    });
                }
            }
        }
    }
}
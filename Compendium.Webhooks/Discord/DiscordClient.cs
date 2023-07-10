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
        private static HttpMethod _patchMethod = new HttpMethod("PATCH");
        private static HttpClient _client;
        private static DateTime? _lastCheck;

        public const string BaseUrl = "https://discord.com/api/v10";

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
                lock (webhook.Lock)
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
                                if (webhook.HasTargetMessage)
                                {
                                    if (!webhook.TargetMessageSet)
                                    {
                                        if (webhook.MessageId > 0)
                                            webhook.TargetMessageSet = true;
                                    }
                                }

                                if (message.PayloadType is DiscordWebhookPayloadType.Post)
                                {
                                    if (webhook.HasTargetMessage && webhook.TargetMessageSet)
                                        return;

                                    var url = $"{BaseUrl}/webhooks/{webhook.Id}/{webhook.Token}";
                                    var bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
                                    var httpContent = new MultipartFormDataContent(bound);
                                    var jsonContent = new StringContent(message.ToJson());

                                    jsonContent.Headers.ContentType = _jsonHeader;

                                    httpContent.Add(jsonContent, "payload_json");

                                    using (var response = await _client.PostAsync(url, httpContent))
                                    {
                                        if (!response.IsSuccessStatusCode)
                                            webhook.Next = DateTime.Now + TimeSpan.FromSeconds(1);
                                        else
                                        {
                                            webhook.Next = null;

                                            if (webhook.HasTargetMessage)
                                                webhook.TargetMessageSet = true;
                                        }
                                    }
                                }
                                else if (message.PayloadType is DiscordWebhookPayloadType.Edit)
                                {
                                    if (!webhook.HasTargetMessage)
                                    {
                                        var url = $"{BaseUrl}/webhooks/{webhook.Id}/{webhook.Token}";
                                        var bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
                                        var httpContent = new MultipartFormDataContent(bound);
                                        var jsonContent = new StringContent(message.ToJson());

                                        jsonContent.Headers.ContentType = _jsonHeader;

                                        httpContent.Add(jsonContent, "payload_json");

                                        using (var request = new HttpRequestMessage(_patchMethod, url))
                                        using (var response = await _client.PostAsync(url, httpContent))
                                        {
                                            if (!response.IsSuccessStatusCode)
                                                webhook.Next = DateTime.Now + TimeSpan.FromSeconds(1);
                                            else
                                                webhook.Next = null;
                                        }
                                    }
                                    else
                                    {
                                        var url = $"{BaseUrl}/webhooks/{webhook.Id}/{webhook.Token}";
                                        var bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
                                        var httpContent = new MultipartFormDataContent(bound);
                                        var jsonContent = new StringContent(message.ToJson());

                                        jsonContent.Headers.ContentType = _jsonHeader;

                                        httpContent.Add(jsonContent, "payload_json");

                                        using (var response = await _client.PostAsync(url, httpContent))
                                        {
                                            if (!response.IsSuccessStatusCode)
                                                webhook.Next = DateTime.Now + TimeSpan.FromSeconds(1);
                                            else
                                            {
                                                webhook.Next = null;

                                                if (webhook.HasTargetMessage)
                                                    webhook.TargetMessageSet = true;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var url = $"{BaseUrl}/webhooks/{webhook.Id}/{webhook.Token}/messages/{message.MessageId}";
                                    var bound = "------------------------" + DateTime.Now.Ticks.ToString("x");
                                    var httpContent = new MultipartFormDataContent(bound);
                                    var jsonContent = new StringContent(message.ToJson());

                                    jsonContent.Headers.ContentType = _jsonHeader;

                                    httpContent.Add(jsonContent, "payload_json");

                                    using (var request = new HttpRequestMessage(HttpMethod.Delete, url))
                                    using (var response = await _client.SendAsync(request))
                                    {
                                        if (!response.IsSuccessStatusCode)
                                            webhook.Next = DateTime.Now + TimeSpan.FromSeconds(2);
                                        else
                                            webhook.Next = null;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                FLog.Error($"Failed to send payload:\n{ex}",
                                    new DebugParameter("payload", message.ToJson()),
                                    new DebugParameter("id", webhook.Id),
                                    new DebugParameter("token", webhook.Token));
                            }
                        });
                    }
                }
            }
        }
    }
}
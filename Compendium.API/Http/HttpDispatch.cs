using helpers;
using helpers.Attributes;
using helpers.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace Compendium.Http
{
    [LogSource("HTTP")]
    public static class HttpDispatch
    {
        private static volatile CancellationTokenSource _tSource;
        private static volatile HttpClient _client;
        private static volatile ConcurrentQueue<HttpDispatchData> _dispatchQueue = new ConcurrentQueue<HttpDispatchData>();

        private static Thread _runThread;

        public static void PostJson(string address, Action<HttpDispatchData> callback, string jsonContent, params KeyValuePair<string, string>[] headers)
            => Post(address, callback, new StringContent(jsonContent), headers);

        public static void Post(string address, Action<HttpDispatchData> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, address);

            if (headers.Any())
            {
                headers.ForEach(pair =>
                {
                    request.Headers.Add(pair.Key, pair.Value);
                });
            }

            if (content != null)
                request.Content = content;

            _dispatchQueue.Enqueue(new HttpDispatchData(address, request, callback));

            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Debug($"Enqueued POST request to {address}.");
        }

        public static void Get(string address, Action<HttpDispatchData> callback, params KeyValuePair<string, string>[] headers)
            => Get(address, callback, null, headers);

        public static void GetJson(string address, Action<HttpDispatchData> callback, string jsonContent, params KeyValuePair<string, string>[] headers)
            => Get(address, callback, new StringContent(jsonContent), headers);

        public static void Get(string address, Action<HttpDispatchData> callback, HttpContent content, params KeyValuePair<string, string>[] headers)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, address);

            if (headers.Any())
            {
                headers.ForEach(pair =>
                {
                    request.Headers.Add(pair.Key, pair.Value);
                });
            }

            if (content != null)
            {
                request.Content = content;
            }

            _dispatchQueue.Enqueue(new HttpDispatchData(address, request, callback));

            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Debug($"Enqueued GET request to {address}.");
        }

        [Load]
        private static void Initialize()
        {
            if (!Plugin.Config.HttpSettings.IsEnabled)
            {
                Plugin.Warn($"HTTP Dispatch is disabled via config! Some features may fail due to this.");
                return;
            }

            _tSource = new CancellationTokenSource();
            _client = new HttpClient();

            _runThread = new Thread(() => ThreadRunner(_tSource.Token));
            _runThread.Start();

            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Debug($"HTTP dispatch client initialized.");
        }

        [Unload]
        private static void Unload()
        {
            _tSource.Cancel();

            _dispatchQueue.Clear();

            _runThread = null;

            _client.Dispose();
            _client = null;

            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Debug($"HTTP dispatch client unloaded.");
        }

        [Reload]
        private static void Reload()
        {
            _dispatchQueue.Clear();

            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Debug($"HTTP dispatch client reloaded.");
        }

        private static void OnRequestFailed(HttpDispatchData httpDispatchData, Exception exception)
        {
            if (Plugin.Config.HttpSettings.Debug)
            {
                Plugin.Warn($"Request to \"{httpDispatchData.Target}\" failed!");
                Plugin.Warn($"{exception.Message}");
            }

            if (Plugin.Config.HttpSettings.MaxRequeueCount is 0)
                return;

            if (httpDispatchData.RequeueCount >= Plugin.Config.HttpSettings.MaxRequeueCount)
                return;

            _dispatchQueue.Enqueue(httpDispatchData);
            httpDispatchData.OnRequeued();
        }

        private static void OnRequestFailed(HttpDispatchData httpDispatchData, HttpStatusCode code)
        {
            if (Plugin.Config.HttpSettings.Debug)
                Plugin.Warn($"Request to \"{httpDispatchData.Target}\" failed! ({code.ToString().SpaceByPascalCase()})");

            if (Plugin.Config.HttpSettings.MaxRequeueCount is 0)
                return;

            if (httpDispatchData.RequeueCount >= Plugin.Config.HttpSettings.MaxRequeueCount)
                return;

            _dispatchQueue.Enqueue(httpDispatchData);
            httpDispatchData.OnRequeued();
        }

        private static async void ThreadRunner(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;

                if (_dispatchQueue.TryDequeue(out var data))
                {
                    try
                    {
                        if (Plugin.Config.HttpSettings.Debug)
                            Plugin.Debug($"Sending {data.Request.Method.Method} request to {data.Request.RequestUri} ..");

                        using (var response = await _client.SendAsync(data.Request))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                OnRequestFailed(data, response.StatusCode);
                                continue;
                            }

                            var result = await response.Content.ReadAsStringAsync();

                            if (Plugin.Config.HttpSettings.Debug)
                                Plugin.Debug($"Received response for {data.Request.Method.Method} request to {data.Request.RequestUri}:\n{result}");

                            data.OnReceived(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnRequestFailed(data, ex);
                    }
                }
            }
        }
    }
}

using Compendium.Features;
using Compendium.Helpers.Calls;

using helpers.Configuration.Ini;
using helpers.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Compendium.ServerGuard.Dispatch
{
    public static class HttpDispatch
    {
        private static readonly ConcurrentQueue<HttpDispatchData> _queue = new ConcurrentQueue<HttpDispatchData>();
        private static HttpClient _client = new HttpClient();

        public static bool IsPaused { get; set; }

        [IniConfig(Name = "Dispatch Requeue", Description = "Whether or not to put failed requests back into the queue.")]
        public static bool Requeue { get; set; } = true;

        public static void Queue(string address, Action<string> callback, params KeyValuePair<string, string>[] headers)
        {
            var data = new HttpDispatchData();

            data.headers = headers;
            data.callback = callback;
            data.address = address.Trim();

            lock (_queue)
                _queue.Enqueue(data);
        }

        public static void OnUpdate()
        {
            if (IsPaused)
                return;

            lock (_queue)
            {
                if (_queue.TryDequeue(out var dispatch))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, dispatch.address);

                            if (dispatch.headers != null && dispatch.headers.Any())
                                dispatch.headers.ForEach(header => request.Headers.Add(header.Key, header.Value));

                            using (var response = await _client.SendAsync(request))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (Requeue)
                                        CallHelper.CallWithDelay(() => _queue.Enqueue(dispatch), 2f);

                                    return;
                                }

                                try
                                {
                                    dispatch.callback?.Invoke(await response.Content.ReadAsStringAsync());
                                }
                                catch { }
                            }
                        }
                        catch
                        {
                            if (Requeue)
                                CallHelper.CallWithDelay(() => _queue.Enqueue(dispatch), 2f);
                        }
                    });
                }
            }
        }
    }
}

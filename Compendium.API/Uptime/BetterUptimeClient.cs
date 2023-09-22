using helpers.Attributes;

using System;
using System.Net.Http;
using System.Timers;

namespace Compendium.Uptime
{
    public static class BetterUptimeClient
    {
        private static HttpClient _client;
        private static Timer _timer;

        [Load]
        [Reload]
        public static void Load()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnFired;
                _timer.Dispose();
                _timer = null;
            }

            if (Plugin.Config.BetterUptimeSettings.BetterUptimeUrl == "none")
                return;

            _client = new HttpClient();
            _timer = new Timer(Plugin.Config.BetterUptimeSettings.Interval);
            _timer.Elapsed += OnFired;
            _timer.Enabled = true;
            _timer.Start();

            Plugin.Info($"Better Uptime client enabled: '{Plugin.Config.BetterUptimeSettings.BetterUptimeUrl}'");
        }

        private static void OnFired(object sender, ElapsedEventArgs ev)
        {
            if (_client is null)
                return;

            try
            {
                using (var message = new HttpRequestMessage(HttpMethod.Post, Plugin.Config.BetterUptimeSettings.BetterUptimeUrl))
                    _client.SendAsync(message);
            }
            catch (Exception ex)
            {
                Plugin.Error($"Failed to send heartbeat: {ex.Message}");
            }
        }
    }
}
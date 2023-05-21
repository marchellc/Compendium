using Compendium.Attributes;
using Compendium.Helpers.Events;

using helpers.Extensions;

using PluginAPI.Enums;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Compendium.Helpers.Timing
{
    public static class EventTimingHelper
    {
        private static readonly Dictionary<ServerEventType, TimingData> _apiTimings = new Dictionary<ServerEventType, TimingData>();
        private static readonly Dictionary<ServerEventType, TimingData> _providerTimings = new Dictionary<ServerEventType, TimingData>();

        [InitOnLoad]
        public static void Initialize()
        {
            foreach (var eventType in Enum
                .GetValues(typeof(ServerEventType))
                .Cast<ServerEventType>())
            {
                _apiTimings[eventType] = new TimingData();
                _providerTimings[eventType] = new TimingData();
            }

            ServerEventType.RoundEnd.GetProvider()?.Add(LogRoundReport);
        }

        public static void Record(Stopwatch stopwatch, ServerEventType type, bool isProvider)
        {
            Plugin.Debug($"Recording event timing: {type};{isProvider};{stopwatch.ElapsedMilliseconds}");

            if (isProvider) _providerTimings[type].Record(stopwatch);
            else _apiTimings[type].Record(stopwatch);
        }

        public static void Reset()
        {
            _apiTimings.ForEach(x => x.Value.Reset());
            _providerTimings.ForEach(x => x.Value.Reset());
        }

        public static void LogRoundReport() => Plugin.Info(CreateReport());
        public static string CreateReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine($"<-- Event Timings Report -->");

            foreach (var evType in Enum
                .GetValues(typeof(ServerEventType))
                .Cast<ServerEventType>())
            {
                var apiTiming = evType.GetData(false);
                var providerTiming = evType.GetData(true);

                sb.AppendLine($">- {evType}  API: {apiTiming.Total} total; {apiTiming.Maximum}ms max; {apiTiming.Minimum}ms min; {apiTiming.Average}ms average");
                sb.AppendLine($"                   Provider: {providerTiming.Total} total; {providerTiming.Maximum}ms max; {providerTiming.Minimum}ms min; {providerTiming.Average}ms average");
            }

            sb.AppendLine($"<-- Event Timings Report -->");
            return sb.ToString();
        }

        public static TimingData GetData(this ServerEventType serverEventType, bool provider) => provider ? _providerTimings[serverEventType] : _apiTimings[serverEventType];
    }
}

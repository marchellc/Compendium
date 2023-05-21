using Compendium.Attributes;
using Compendium.Helpers.Events;
using Compendium.Helpers.Patching;
using helpers;
using helpers.Events;

using PluginAPI.Enums;

using System;

using UnityEngine;
using UnityEngine.Profiling;

namespace Compendium.Helpers.Timing
{
    public static class FrameUpdateHelper
    {
        private static readonly FrameTimerHelper m_TimeHelper = new FrameTimerHelper();

        public static FrameTimerHelper Timing { get => m_TimeHelper; }

        public static readonly EventProvider OnUpdateEvent = new EventProvider();
        public static readonly EventProvider OnLateUpdateEvent = new EventProvider();
        public static readonly EventProvider OnFixedUpdateEvent = new EventProvider();

        [InitOnLoad]
        public static void Initialize()
        {
            Reflection.AddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
            Reflection.AddHandler<Action>(typeof(StaticUnityMethods), "OnLateUpdate", OnLateUpdate);
            Reflection.AddHandler<Action>(typeof(StaticUnityMethods), "OnFixedUpdate", OnFixedUpdate);

            ServerEventType.RoundEnd.GetProvider()?.Add(LogRoundReport);
        }

        public static string CreateReport () => 
            $"\n" +
            $"<--- Frame Timing Report --->\n" +
            $">- Total Frames: {Timing.ExecutedFrames} frames -<\n" +
            $">- Average Frame Time: {Timing.AverageFrameDurationExact} ms -<\n" +
            $">- Average Frame Time Ticks: {Timing.AverageFrameDurationTicksExact} ticks -<\n" +
            $">- Last Frame Time: {Timing.LastFrameDurationExact} ms -<\n" +
            $">- Last Frame Time Ticks: {Timing.LastFrameDurationTicksExact} ticks -<\n" +
            $">- Maximum Frame Time: {Timing.MaxFrameDurationExact} ms -<\n" +
            $">- Maximum Frame Time Ticks: {Timing.MaxFrameDurationTicksExact} ticks -<\n" +
            $">- Minimum Frame Time: {Timing.MinFrameDurationExact} ms  -<\n" +
            $">- Minimum Frame Time Ticks: {Timing.MinFrameDurationTicksExact} ticks -<\n" +
            $">- Ticks Per Second: {Timing.TicksPerSecondExact} ticks -<\n" +
            $">- Maximum Ticks Per Second: {Timing.MaxTicksPerSecondExact} -<\n" +
            $">- Minimum Ticks Per Second: {Timing.MinTicksPerSecondExact} -<\n" +
            $">- Average Ticks Per Second: {Timing.AverageTicksPerSecondExact} -<\n" +
            $"<--- Frame Timing Report --->";

        public static void LogRoundReport() => Plugin.Info(CreateReport());
        private static void OnUpdate()
        {
            m_TimeHelper.UpdateStats();
            OnUpdateEvent.Invoke();
        }

        private static void OnLateUpdate() => OnLateUpdateEvent.Invoke();
        private static void OnFixedUpdate() => OnFixedUpdateEvent.Invoke();
    }
}

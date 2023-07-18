using Compendium.Helpers.Round;

using helpers;

using System;
using System.Linq;

namespace Compendium.Gameplay.Broadcasts
{
    public static class BroadcastHandler
    {
        private static DateTime? _lastBc;

        public static void Initialize()
        {
            Reflection.TryAddHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
        }

        public static void Unload()
        {
            Reflection.TryRemoveHandler<Action>(typeof(StaticUnityMethods), "OnUpdate", OnUpdate);
        }

        public static void Reload()
        {
            _lastBc = null;
        }

        private static void DoLastBroadcast(ReferenceHub lastPlayer)
        {
            Broadcast.Singleton?.TargetClearElements(lastPlayer.connectionToClient);
            Broadcast.Singleton?.TargetAddElement(lastPlayer.connectionToClient, $"", 10, Broadcast.BroadcastFlags.Normal);
        }

        private static bool CanBroadcastLast(out ReferenceHub lastPlayer)
        {
            if (!RoundHelper.TryGenerateEndPreventingPlayerList(out var endPreventingPlayers) || endPreventingPlayers.Count != 1)
            {
                lastPlayer = null;
                return false;
            }

            lastPlayer = endPreventingPlayers.First();
            return true;
        }

        private static void OnUpdate()
        {
            try
            {
                if (!RoundSummary.RoundInProgress())
                    return;
            }
            catch { return; }

            if (!CanBroadcastLast(out var lastPlayer))
                return;

            if (_lastBc.HasValue)
            {
                if ((DateTime.Now - _lastBc.Value).Seconds < LastBroadcastProperties.Duration)
                    return;
            }

            _lastBc = DateTime.Now;
            DoLastBroadcast(lastPlayer);
        }
    }
}
using Compendium.Round;

using helpers;

using System;
using System.Linq;

namespace Compendium.Gameplay.Broadcasts
{
    public static class BroadcastHandler
    {
        private static DateTime? _lastBc;

        public static void Reload()
        {
            _lastBc = null;
        }

        private static void DoLastBroadcast(ReferenceHub lastPlayer)
        {
            Broadcast.Singleton?.TargetClearElements(lastPlayer.connectionToClient);
            Broadcast.Singleton?.TargetAddElement(lastPlayer.connectionToClient, $"", 10, Broadcast.BroadcastFlags.Normal);
        }
    }
}
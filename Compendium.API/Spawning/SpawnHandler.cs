using Compendium.Attributes;
using Compendium.Collections;
using Compendium.Events;

using PluginAPI.Events;

using System;

namespace Compendium.Spawning
{
    public static class SpawnHandler
    {
        public static event Action<ReferenceHub, SpawnInfo> OnChosen;
        public static event Action<ReferenceHub, SpawnInfo> OnSpawned;

        public static SafeAccessDictionary<ReferenceHub, SpawnInfo> Chosen { get; } = new SafeAccessDictionary<ReferenceHub, SpawnInfo>();

        [Event]
        private static void OnPlayerJoined(PlayerJoinedEvent ev)
        {
            if (RoundHelper.State != Enums.RoundState.WaitingForPlayers)
                return;

            if (ev.Player is null || ev.Player.IsServer)
                return;

            var infoRole = SpawnRoleChooser.Choose(ev.Player.ReferenceHub);
            var infoRoom = SpawnPositionChooser.Choose(ev.Player.ReferenceHub);
            var infoPos = SpawnPositionChooser.GetPosition(infoRoom);

            Plugin.Debug($"Selected spawn position and role for '{ev.Player.ReferenceHub.GetLogName()}': {infoRole} | {infoRoom.Name} | {infoPos}");

            Chosen[ev.Player.ReferenceHub] = new SpawnInfo(infoRole, infoPos, infoRoom);

            OnChosen?.Invoke(ev.Player.ReferenceHub, Chosen[ev.Player.ReferenceHub]);
        }

        [RoundStateChanged(Enums.RoundState.WaitingForPlayers)]
        private static void OnWaiting()
            => Chosen.Clear();
    }
}
using Compendium.Features;
using Compendium.Helpers.Events;
using Compendium.ServerGuard.AccountShield;
using Compendium.ServerGuard.Dispatch;
using Compendium.ServerGuard.VpnShield;

using PluginAPI.Enums;
using PluginAPI.Events;

using System;

namespace Compendium.ServerGuard
{
    public class ServerGuardFeature : ConfigFeatureBase
    {
        public override string Name => "Server Guard";

        public override void CallUpdate()
        {
            base.CallUpdate();
            HttpDispatch.OnUpdate();
        }

        public override void Restart()
        {
            base.Restart();
            HttpDispatch.IsPaused = true;
        }

        public override void OnWaiting()
        {
            base.OnWaiting();
            HttpDispatch.IsPaused = false;
        }

        public override void Load()
        {
            base.Load();
            VpnShieldHandler.Initialize();
            ServerEventType.PlayerJoined.AddHandler<Action<PlayerJoinedEvent, ValueContainer, ValueContainer>>(OnPlayerJoined);
        }

        public override void Unload()
        {
            base.Unload();
            ServerEventType.PlayerJoined.RemoveHandler<Action<PlayerJoinedEvent, ValueContainer, ValueContainer>>(OnPlayerJoined);
        }

        private static void OnPlayerJoined(PlayerJoinedEvent ev, ValueContainer result, ValueContainer execute)
        {
            VpnShieldHandler.Check(ev.Player.ReferenceHub, isKicked =>
            {
                if (isKicked)
                    return;

                AccountShieldHandler.Check(ev.Player.ReferenceHub);
            });
        }
    }
}
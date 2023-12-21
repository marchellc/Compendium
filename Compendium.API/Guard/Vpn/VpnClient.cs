using Compendium.Http;

using System;
using System.Text.Json;
using System.Collections.Generic;

namespace Compendium.Guard.Vpn
{
    public class VpnClient : ServerGuardClient
    {
        public const string BaseUrl = "http://v2.api.iphub.info/ip";

        public override bool TryCheck(ReferenceHub hub)
        {
            if (!base.TryCheck(hub))
                return false;

            var url = $"{BaseUrl}/{hub.Ip()}";

            HttpDispatch.Get(url, data =>
            {
                try
                {
                    var response = JsonSerializer.Deserialize<VpnResponse>(data.Response);
                    var id = hub.UserId();
                    var ip = hub.Ip();

                    if (response.BlockLevel == 0)
                    {
                        ServerGuard.Safe(id, ip);
                        return;
                    }

                    if (response.BlockLevel == 1 || (response.BlockLevel == 2 && Plugin.Config.GuardSettings.VpnStrictMode))
                    {
                        Plugin.Warn($"Kicked and flagged user '{hub.Nick()}' (UID: {hub.UserId()}; IP: {hub.Ip()}) due to having a positive VPN check result (status: {response.BlockLevel})");

                        hub.Kick(
                            $"Byl jsi vyhozen kvůli detekované VPN / proxy síti.\n" +
                            $"Vypni VPN program a zkus se připojit znova." +
                            $"\nPokud nevyužíváš žádný VPN program, tak se připoj na náš Discord server (adresa je v infu) a podej si žádost o whitelist v kanále #support.");

                        ServerGuard.Flag(id, ip);
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Error($"Failed to retrieve IP hub info");
                    Plugin.Error(ex);
                }
            }, new KeyValuePair<string, string>("X-Key", Plugin.Config.GuardSettings.VpnClientKey));

            return true;
        }
    }
}
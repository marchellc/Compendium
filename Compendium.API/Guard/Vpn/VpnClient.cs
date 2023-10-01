using Compendium.Http;

using System.Collections.Generic;
using System;
using System.Text.Json;

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
                    Plugin.Debug(data.Response);

                    var response = JsonSerializer.Deserialize<VpnResponse>(data.Response);
                    var id = hub.UserId();
                    var ip = response.Ip;

                    if (response.BlockLevel == 0)
                    {
                        ServerGuard.Safe(id, ip);
                        return;
                    }

                    if (response.BlockLevel == 1 || (response.BlockLevel == 2 && Plugin.Config.GuardSettings.VpnStrictMode))
                        hub.Kick(Plugin.Config.GuardSettings.VpnKickMessage);

                    ServerGuard.Flag(id, ip);
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
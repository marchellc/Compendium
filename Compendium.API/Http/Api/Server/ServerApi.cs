using Compendium.HttpServer;

using Grapevine;

using PluginAPI.Core;

using System.Threading.Tasks;

namespace Compendium.HttpApi
{
    [RestResource]
    public class ServerApi
    {
        [RestRoute("Get", "api/server/bu_status")]
        public async Task UptimeRoute(IHttpContext context)
        {
            if (!context.TryAccess())
                return;

            context.Respond("OK");
        }

        [RestRoute("Get", "/api/server/status")]
        public async Task ServerStatusAsync(IHttpContext context)
        {
            if (!context.TryAccess("server.status"))
                return;

            context.RespondJson(ServerStatusObject.GetCurrent());
        }

        [RestRoute("Any", "/api/server/restart")]
        public async Task ServerRestartAsync(IHttpContext context)
        {
            if (!context.TryAccess("server.restart"))
                return;

            World.Broadcast($"<color=red><b>Server se restartuje za 10 sekund!</b></color>", 10, true);
            Calls.Delay(10f, () => Server.Restart());

            context.Respond("The server is going to restart in 10 seconds ..");
        }
    }
}

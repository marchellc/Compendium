using Compendium.HttpServer;

using Grapevine;

using System.Threading.Tasks;

namespace Compendium.HttpApi
{
    [RestResource]
    public class RoundApi
    {
        [RestRoute("Any", "api/round/restart")]
        public async Task RoundRestartAsync(IHttpContext context)
        {
            if (!context.TryAccess("round.restart"))
                return;

            PluginAPI.Core.Round.Restart(false, false, ServerStatic.NextRoundAction.DoNothing);
            context.Respond("The round is restarting ..");
        }
    }
}
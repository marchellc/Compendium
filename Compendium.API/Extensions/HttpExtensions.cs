using Compendium.HttpServer.Authentification;
using Compendium.HttpServer.Responses;

using Grapevine;

namespace Compendium.HttpServer
{
    public static class HttpExtensions
    {
        public static void RespondFail(this IHttpContext context, System.Net.HttpStatusCode code, string message = null)
            => ResponseData.Respond(context, new ResponseData
            {
                Code = (int)code,
                IsSuccess = false,
                Data = message
            });

        public static void Respond(this IHttpContext context, string response = null)
            => ResponseData.Respond(context, ResponseData.Ok(response));

        public static void RespondJson(this IHttpContext context, object response)
            => ResponseData.Respond(context, ResponseData.Ok(response));

        public static bool TryAccess(this IHttpContext context, string perm = null)
            => string.IsNullOrWhiteSpace(perm) || context.TryAuth(perm);

        public static bool TryAuth(this IHttpContext context, string perm)
        {
            var key = context.Request.Headers.GetValue<string>("X-Key");

            if (string.IsNullOrWhiteSpace(key))
            {
                ResponseData.Respond(context, ResponseData.MissingKey());
                Plugin.Warn($"{context.Request.RemoteEndPoint} attempted to access '{context.Request.Endpoint}' without an auth key!");
                return false;
            }

            var authRes = HttpAuthentificator.TryAuthentificate(key, perm);

            if (authRes != HttpAuthentificationResult.Authorized)
            {
                ResponseData.Respond(context, ResponseData.InvalidKey());
                Plugin.Warn($"{context.Request.RemoteEndPoint} attempted to access '{context.Request.Endpoint}' with an '{authRes}' auth!");
                return false;
            }

            Plugin.Debug($"Authorized access to '{context.Request.Endpoint}' for '{context.Request.RemoteEndPoint}'");
            return true;
        }
    }
}
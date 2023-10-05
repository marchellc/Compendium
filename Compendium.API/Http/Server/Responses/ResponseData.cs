using Grapevine;

using System.Text.Json;
using System.Text.Json.Serialization;

using HttpStatusCode = System.Net.HttpStatusCode;

namespace Compendium.HttpServer.Responses
{
    public class ResponseData
    {
        [JsonPropertyName("res_code")]
        public int Code { get; set; } = 0;

        [JsonPropertyName("res_success")]
        public bool IsSuccess { get; set; } = false;

        [JsonPropertyName("res_data")]
        public string Data { get; set; } = null;

        public static ResponseData MissingKey()
        {
            return new ResponseData()
            {
                Code = (int)HttpStatusCode.Unauthorized,
                IsSuccess = false,
                Data = "Missing authorization header!"
            };
        }

        public static ResponseData InvalidKey()
        {
            return new ResponseData()
            {
                Code = (int)HttpStatusCode.Unauthorized,
                IsSuccess = false,
                Data = "Invalid authorization key!"
            };
        }

        public static ResponseData Ok(string response = null)
        {
            return new ResponseData()
            {
                Code = (int)HttpStatusCode.OK,
                IsSuccess = true,
                Data = response
            };
        }

        public static ResponseData Ok(object response = null)
        {
            string res = null;

            if (response != null)
                res = JsonSerializer.Serialize(response);

            return Ok(res);
        }

        public static ResponseData Fail(string response = null)
        {
            return new ResponseData()
            {
                Code = (int)HttpStatusCode.Forbidden,
                IsSuccess = false,
                Data = response
            };
        }

        public static void Respond(IHttpContext context, ResponseData data)
            => context.Response.SendResponseAsync(JsonSerializer.Serialize(data));
    }
}
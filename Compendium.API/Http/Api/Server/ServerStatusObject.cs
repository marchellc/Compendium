using Compendium.Enums;

using GameCore;

using System.Text.Json.Serialization;

namespace Compendium.HttpApi
{
    public class ServerStatusObject
    {
        [JsonPropertyName("server_name")]
        public string Name { get; set; }

        [JsonPropertyName("server_players")]
        public int Players { get; set; }

        [JsonPropertyName("server_max_players")]
        public int MaxPlayers { get; set; }

        [JsonPropertyName("server_status_id")]
        public int StatusId { get; set; }

        public static ServerStatusObject GetCurrent()
        {
            var obj = new ServerStatusObject();

            obj.Name = World.CurrentClearOrAlternativeServerName;
            obj.Players = Hub.Count;
            obj.MaxPlayers = ConfigFile.ServerConfig.GetInt("max_players", 20);
            obj.StatusId = GetCurrentStatusId();

            return obj;
        }

        public static int GetCurrentStatusId()
        {
            if (IdleMode.IdleModeActive)
                return (int)ServerStatusId.Idle;

            if (RoundHelper.State is RoundState.Ending)
                return (int)ServerStatusId.RoundEnding;

            if (RoundHelper.State is RoundState.InProgress)
                return (int)ServerStatusId.RoundInProgress;

            if (RoundHelper.State is RoundState.Restarting)
                return (int)ServerStatusId.RoundRestarting;

            return (int)ServerStatusId.WaitingForPlayers;
        }
    }
}
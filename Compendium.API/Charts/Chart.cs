using System.Text.Json.Serialization;

namespace Compendium.Charts
{
    public class Chart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public ChartData Data { get; set; }
    }
}
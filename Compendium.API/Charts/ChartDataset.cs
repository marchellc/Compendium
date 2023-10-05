using System.Text.Json.Serialization;

namespace Compendium.Charts
{
    public class ChartDataset
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("data")]
        public int[] Data { get; set; }
    }
}
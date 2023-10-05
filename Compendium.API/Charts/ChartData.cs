using System.Text.Json.Serialization;

namespace Compendium.Charts
{
    public class ChartData
    {
        [JsonPropertyName("labels")]
        public string[] Labels { get; set; }

        [JsonPropertyName("datasets")]
        public ChartDataset[] Datasets { get; set; }
    }
}
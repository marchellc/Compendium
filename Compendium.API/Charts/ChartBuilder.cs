using System.Collections.Generic;
using System.Text.Json;

namespace Compendium.Charts
{
    public static class ChartBuilder
    {
        public static byte[] GetChart(string label, IEnumerable<KeyValuePair<string, int>> data)
            => BuildHorizontalBarChart(label, data).ToByteArray();

        public static QuickChart.Chart BuildHorizontalBarChart(string label, IEnumerable<KeyValuePair<string, int>> data)
            => BuildChart("horizontalBar", label, data);

        public static QuickChart.Chart BuildChart(string type, string label, IEnumerable<KeyValuePair<string, int>> data)
        {
            var chart = new Chart();
            var chartData = new ChartData();
            var dataset = new ChartDataset();

            chart.Type = type;

            var labelList = new List<string>();
            var dataList = new List<int>();

            foreach (var p in data)
            {
                labelList.Add(p.Key);
                dataList.Add(p.Value);
            }

            dataset.Label = label;
            dataset.Data = dataList.ToArray();

            chartData.Labels = labelList.ToArray();
            chartData.Datasets = new ChartDataset[] { dataset };

            chart.Data = chartData;

            var chartJson = JsonSerializer.Serialize(chart);
            var qChart = new QuickChart.Chart();

            qChart.Width = 1440;
            qChart.Height = 1024;
            qChart.Version = "2.9.4";
            qChart.Config = chartJson;

            return qChart;
        }
    }
}

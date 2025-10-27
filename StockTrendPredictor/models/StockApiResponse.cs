using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

namespace StockTrendPredictor.Models
{
    public class StockApiResponse
    {
        [JsonPropertyName("Meta Data")]
        public required MetaData MetaData { get; set; }

        [JsonPropertyName("Time Series (Daily)")]
        public required Dictionary<string, DailyStockData> TimeSeriesDaily { get; set; }
        // Dictionary representerar kollektion av värden.
        // string = datumet i JSON-data, DailyStockData är värdena.
    }

    public class MetaData
    {
        [JsonPropertyName("1. Information")] public required string Information { get; set; }
        [JsonPropertyName("2. Symbol")] public required string Symbol { get; set; }
        [JsonPropertyName("3. Last Refreshed")] public string? LastRefreshed { get; set; }
    }

    public class DailyStockData
    {
        [JsonPropertyName("1. open")] public string? Open { get; set; }
        [JsonPropertyName("2. high")] public string? High { get; set; }
        [JsonPropertyName("3. low")] public string? Low { get; set; }
        [JsonPropertyName("4. close")] public string? Close { get; set; }
        [JsonPropertyName("5. volume")] public string? Volume { get; set; }
    }
}
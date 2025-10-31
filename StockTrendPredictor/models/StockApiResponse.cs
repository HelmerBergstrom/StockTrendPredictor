using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;

//<summary>
// Nedan är exempel på hur data från företaget NVIDIA ser ut. 
// Data från API:et kan se ut som följande:

// {
//     "Meta Data": {
//         "1. Information": "Daily Prices (open, high, low, close) and Volumes",
//         "2. Symbol": "NVDA",
//         "3. Last Refreshed": "2025-10-24",
//         "4. Output Size": "Compact",
//         "5. Time Zone": "US/Eastern"
//     },
//     "Time Series (Daily)": {
//         "2025-10-24": {
//             "1. open": "183.8350",
//             "2. high": "187.4700",
//             "3. low": "183.5000",
//             "4. close": "186.2600",
//             "5. volume": "131296677"
//         },
// </summary>


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
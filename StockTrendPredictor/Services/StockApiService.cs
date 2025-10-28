using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using StockTrendPredictor.Models;

namespace StockTrendPredictor.Services
{
    public class StockApiService
    {
        private readonly HttpClient client = new();
        private readonly string _apiKey = "4F5TKNYGFANV6O5U";

        public async Task<List<StockData>> GetStockDataAsync(string Symbol)
        {
            // deklarerar variabel med URL:en.
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={Symbol}&apikey={_apiKey}";
            // GET-begäran till API:et. Await för att invänta detta innan vi går vidare.
            var json = await client.GetStringAsync(url);

            // Serialiserar data från API:et, för att göra om data till C#-objekt ist för JSON.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            StockApiResponse? apiResponse = JsonSerializer.Deserialize<StockApiResponse>(json, options);

        }
    }

}
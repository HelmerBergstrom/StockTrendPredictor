using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Microsoft.ML.Transforms;
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
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={Symbol}&outputsize=full&apikey={_apiKey}";
            // GET-begäran till API:et. Await för att invänta detta innan vi går vidare.
            var json = await client.GetStringAsync(url);

            // Om gränsen för API-förfrågningar på en dag är nådd, stannar det här.
            if (json.Contains("Please subscribe") || json.Contains("API rate limit is 25 requests per day"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gränsen är nådd för API-förfrågningar idag. Försök igen imorgon.");
                return new List<StockData>();
            }

            // Serialiserar data från API:et, för att göra om data till C#-objekt ist för JSON.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            StockApiResponse? apiResponse = JsonSerializer.Deserialize<StockApiResponse>(json, options);

            // Kontroll om det finns data i API:et. Finns det inte körs if-satsen.
            if (apiResponse?.TimeSeriesDaily == null)
            {
                Console.WriteLine("Kunde inte hämta data från API:et. Kontrollera API-nyckeln och försök igen!");
                // Avslutar metoden helt. Returnerar en tom lista, eftersom koden ovan alltid får en giltlig uppsättning av listan.
                return new List<StockData>();
            }

            // Konverterar dictionary till StockData-lista.
            // "kvp" står för KeyValuePair i denna metod.
            var list = apiResponse.TimeSeriesDaily
                .Select(kvp =>
                {
                    var Date = DateTime.Parse(kvp.Key);
                    var d = kvp.Value;
                    return new StockData
                    {
                        // float likt StockData-class. 
                        // Frågetecken då det kan vara null.
                        Date = Date,
                        Open = float.Parse(d.Open ?? "0"),
                        High = float.Parse(d.High ?? "0"),
                        Low = float.Parse(d.Low ?? "0"),
                        Close = float.Parse(d.Close ?? "0"),
                        Volume = float.Parse(d.Volume ?? "0"),
                    };
                })
                .OrderBy(d => d.Date) // sorterar utifrån datum.
                .ToList(); // list = List<StockData>

            return list;
        }
        
    }

}
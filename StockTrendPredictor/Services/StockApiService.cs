using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockTrendPredictor.Models;

namespace StockTrendPredictor.Services
{
    public class StockApiService
    {
        private readonly HttpClient client = new();
        private readonly string? _apiKey;

        public StockApiService()
        {
            // Läser API-nyckeln från appsettings.json (ignoreras av git) eller miljövariabeln ALPHAVANTAGE_API_KEY.
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _apiKey = Environment.GetEnvironmentVariable("ALPHAVANTAGE_API_KEY") ?? config["AlphaVantage:ApiKey"];
        }

        public async Task<List<StockData>> GetStockDataAsync(string Symbol)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Ingen API-nyckel hittades. Lägg till den i appsettings.json eller miljövariabeln ALPHAVANTAGE_API_KEY.");
                return new List<StockData>();
            }

            // deklarerar variabel med URL:en.
            // outputsize=compact (senaste 100 dagarna). "full" kräver numera ett premiumabonnemang hos Alpha Vantage.
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={Uri.EscapeDataString(Symbol)}&outputsize=compact&apikey={_apiKey}";

            string json;
            try
            {
                // GET-begäran till API:et. Await för att invänta detta innan vi går vidare.
                json = await client.GetStringAsync(url);
            }
            catch (HttpRequestException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Kunde inte ansluta till API:et: {ex.Message}");
                return new List<StockData>();
            }

            // Om symbol inte finns.
            if (json.Contains("Error Message") || json.Contains("Invalid API call"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Felaktig symbol. Besök menyval 3 för att se exempel på symboler!");
                return new List<StockData>();
            }

            // Om gränsen för API-förfrågningar på en dag är nådd, stannar det här.
            if (json.Contains("25 requests per day"))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Gränsen är nådd för API-förfrågningar idag. Försök igen imorgon.");
                return new List<StockData>();
            }

            // Övriga meddelanden från Alpha Vantage (t.ex. premiumfunktioner eller för många anrop per sekund)
            // skickas i fälten "Information" eller "Note" istället för aktiedata.
            using (var doc = JsonDocument.Parse(json))
            {
                foreach (var field in new[] { "Information", "Note" })
                {
                    if (doc.RootElement.TryGetProperty(field, out var message))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"API:et returnerade ingen data: {message.GetString()}");
                        return new List<StockData>();
                    }
                }
            }

            // Serialiserar data från API:et, för att göra om data till C#-objekt ist för JSON.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            StockApiResponse? apiResponse;
            try
            {
                apiResponse = JsonSerializer.Deserialize<StockApiResponse>(json, options);
            }
            catch (JsonException)
            {
                apiResponse = null;
            }

            // Kontroll om det finns data i API:et. Finns det inte körs if-satsen.
            if (apiResponse?.TimeSeriesDaily == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Kunde inte hämta data från API:et. Kontrollera API-nyckeln och försök igen!");
                // Avslutar metoden helt. Returnerar en tom lista, eftersom koden ovan alltid får en giltlig uppsättning av listan.
                return new List<StockData>();
            }

            // Konverterar dictionary till StockData-lista.
            // "kvp" står för KeyValuePair i denna metod.
            var list = apiResponse.TimeSeriesDaily
                .Select(kvp =>
                {
                    var Date = DateTime.Parse(kvp.Key, CultureInfo.InvariantCulture);
                    var d = kvp.Value;
                    var high = ParseFloat(d.High);
                    var low = ParseFloat(d.Low);
                    return new StockData
                    {
                        // float likt StockData-class.
                        // Frågetecken då det kan vara null.
                        Date = Date,
                        Open = ParseFloat(d.Open),
                        High = high,
                        Low = low,
                        Close = ParseFloat(d.Close),
                        Volume = ParseFloat(d.Volume),
                        // Samma beräkning som vid träningen i MLService.
                        DailyRange = high - low,
                    };
                })
                .OrderBy(d => d.Date) // sorterar utifrån datum.
                .ToList(); // list = List<StockData>

            return list;
        }

        // API:et använder punkt som decimaltecken. InvariantCulture gör att det tolkas rätt även med svenska inställningar.
        private static float ParseFloat(string? value)
        {
            return float.Parse(value ?? "0", CultureInfo.InvariantCulture);
        }
    }

}

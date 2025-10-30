using System.Net.Http.Headers;
using StockTrendPredictor.Models;
using StockTrendPredictor.Services;


Console.WriteLine("=== Stock Trend Predictor ===");
Console.Write("Ange en aktiekod (ex: AAPL, MSFT, NVDA): ");
string? symbol = Console.ReadLine()?.Trim().ToUpper();

if (string.IsNullOrEmpty(symbol))
{
    Console.WriteLine("Ingen symbol angiven. Avslutar.");
    return;
}

// var service = new StockApiService();
// var stockData = await service.GetStockDataAsync(symbol);

Console.WriteLine($"\nData för {symbol}:");
Console.WriteLine("------------------------------------");

// Console.WriteLine("\nTränar modeller (detta kan ta upp till en minut)...");
// var mlService = new MLService();

// kod för att köra träning av modeller.
// mlService.TrainAndEvaluate(stockData); 
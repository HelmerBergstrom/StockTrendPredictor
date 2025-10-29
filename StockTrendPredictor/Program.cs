using System.Net.Http.Headers;
using StockTrendPredictor.Models;
using StockTrendPredictor.Services;


Console.WriteLine("=== Stock Trend Predictor ===");
Console.Write("Ange en aktiekod (ex: AAPL, MSFT, NVDA): ");
string? symbol = Console.ReadLine()?.ToUpper();

if (string.IsNullOrEmpty(symbol))
{
    Console.WriteLine("Ingen symbol angiven. Avslutar.");
    return;
}

var service = new StockApiService();
var stockData = await service.GetStockDataAsync(symbol);

Console.WriteLine($"\nData för {symbol}:");
Console.WriteLine("------------------------------------");

var mlService = new MLService();
mlService.TrainAndEvaluate(stockData);
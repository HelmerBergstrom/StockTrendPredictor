using System.Net.Http.Headers;
using System.Text.Json.Nodes;
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



var service = new StockApiService();
var stockData = await service.GetStockDataAsync(symbol);

if (stockData.Count == 0)
{
    return;
}

Console.WriteLine($"\nData för {symbol}:");
Console.WriteLine("------------------------------------");

var mlService = new MLService();
var regressionModel = mlService.LoadRegressionModel();
var binaryModel = mlService.LoadBinaryModel();

// Console.WriteLine("\nTränar modeller (detta kan ta upp till en minut)...");
// kod för att köra träning av modeller.
// mlService.TrainAndEvaluate(stockData); 
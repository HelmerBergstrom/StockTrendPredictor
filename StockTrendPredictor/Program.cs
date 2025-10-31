using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.ML;
using StockTrendPredictor.Models;
using StockTrendPredictor.Services;

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("=============================");
Console.WriteLine("=== Stock Trend Predictor ===");
Console.WriteLine("=============================");
Console.Write("\nAnge en aktiekod (ex: AAPL, MSFT, NVDA): ");
string? symbol = Console.ReadLine()?.Trim().ToUpper();

if (string.IsNullOrEmpty(symbol))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Ingen symbol angiven. Avslutar.");
    return;
}

var service = new StockApiService();
var stockData = await service.GetStockDataAsync(symbol);

if (stockData.Count == 0)
{
    return;
}

// Laddar modeller.
var mlService = new MLService();
var regressionModel = mlService.LoadRegressionModel();
var binaryModel = mlService.LoadBinaryModel();

// Förutsägning av nästa dags stängningskurs (regressionsmodell).
var latestData = stockData.Last();
var mlContext = new MLContext();
var predictionEngine = mlContext.Model
    .CreatePredictionEngine<StockData, StockPrediction>(regressionModel);
var predictedClose = predictionEngine.Predict(latestData);
var yesterdaysClose = latestData.Close;

// Förutsägning av nästa dags upp/nedgång. Klassificeringsmodellen.
var binaryPredictionEngine = mlContext.Model
    .CreatePredictionEngine<StockData, StockDirectionPrediction>(binaryModel);
var predictedDirection = binaryPredictionEngine.Predict(latestData);

string direction = predictedDirection.PredictedLabel ? "📈 Upp" : "📉 Ned";

// Gör om probability till heltal.
var probabilityToInt = predictedDirection.Probability * 100;

string upOrDown = "";


// Är förutsägelsen om stängningskursen mer eller mindre än gårdagens stängning?
if (predictedClose.PredictedClose > yesterdaysClose)
{
    Console.ForegroundColor = ConsoleColor.Green;
    upOrDown = "UPPGÅNG";
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    upOrDown = "NEDGÅNG"; 
}

Console.WriteLine("\n==========================================================");
Console.WriteLine("========   R E G R E S S I O N S M O D E L L E N   ========");
Console.WriteLine("==========================================================\n");

// Skriver ut förutsägelsen med ett heltal och 2 decimaler (F2).
Console.WriteLine($"{upOrDown} för aktien ({symbol}) till kursen: {predictedClose.PredictedClose:F2}");

if (probabilityToInt > 49)
{
    Console.ForegroundColor = ConsoleColor.Green;
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
}

Console.WriteLine("\n=================================================================");
Console.WriteLine("========   K L A S S I F I C E R I N G S M O D E L L E N   ========");
Console.WriteLine("=================================================================\n");

Console.WriteLine($"Förväntad rörelse imorgon: {direction}");
Console.WriteLine($"Säkerhet: {probabilityToInt}%");

// Console.WriteLine("\nTränar modeller (detta kan ta upp till en minut)...");
// kod för att köra träning av modeller.
// mlService.TrainAndEvaluate(stockData); 
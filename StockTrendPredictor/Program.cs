using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.ML;
using StockTrendPredictor.Models;
using StockTrendPredictor.Services;

bool running = true;

while (running)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n=============================");
    Console.WriteLine("=== Stock Trend Predictor ===");
    Console.WriteLine("=============================");
    Console.WriteLine("\n1. Gör en ny förutsägelse");
    Console.WriteLine("2. Visa historik");
    Console.WriteLine("3. Visa populära sökningar");
    Console.WriteLine("4. Avsluta");
    Console.Write("\nVälj ett alternativ (1-4): ");

    var key = Console.ReadKey(intercept: true).Key;
    Console.Clear();

    switch (key)
    {
        // D1 = tangenten 1 ovanför Q.
        // NumPad1 = tangenten 1 i keypad/sifforsblocket.
        case ConsoleKey.D1:
        case ConsoleKey.NumPad1:
            await RunPrediction();
            break;

        case ConsoleKey.D2:
        case ConsoleKey.NumPad2:
            ShowPredictionHistory();
            break;

        case ConsoleKey.D3:
        case ConsoleKey.NumPad3:
            ShowPopularStocks();
            break;

        case ConsoleKey.D4:
        case ConsoleKey.NumPad4:
            running = false;
            Console.WriteLine("Avslutar programmet..");
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ogiltigt val, klicka på valfri tangent för att försöka på nytt!");
            Console.ReadKey(intercept: true);
            break;
    }
}

static async Task RunPrediction()
{
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

    string direction = predictedDirection.PredictedLabel ? "Upp" : "Ned";

    float confidence = predictedDirection.PredictedLabel ? predictedDirection.Probability : 1 - predictedDirection.Probability;

    var probabilityToInt = predictedDirection.Probability;

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
    Console.WriteLine("=======   R E G R E S S I O N S M O D E L L E N   ========");
    Console.WriteLine("==========================================================\n");

    // Skriver ut förutsägelsen med ett heltal och 2 decimaler (F2).
    Console.WriteLine($"{upOrDown} för aktien ({symbol}) till kursen: {predictedClose.PredictedClose:F2}");

    if (direction == "Upp")
    {
        Console.ForegroundColor = ConsoleColor.Green;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }

    Console.WriteLine("\n=================================================================");
    Console.WriteLine("=======   K L A S S I F I C E R I N G S M O D E L L E N   =======");
    Console.WriteLine("=================================================================\n");

    Console.WriteLine($"Förväntad rörelse imorgon: {direction}");
    Console.WriteLine($"Säkerhet: {confidence * 100:F1}%");

    var storage = new PredictionStorageService();
    var record = new PredictionRecord
    {
        Symbol = symbol,
        Date = DateTime.Now,
        PredictedClose = predictedClose.PredictedClose,
        PredictedDirection = direction,
        Probability = (float)probabilityToInt
    };

    storage.SavePrediction(record);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\nFörutsägelsen sparades i predictions.json");
    Console.ResetColor();
    Console.WriteLine("\nTryck på en valfri tangent för att återgå till menyn.");
    Console.ReadKey();
}
static void ShowPredictionHistory()
{
    var storage = new PredictionStorageService();
    var predictions = storage.LoadPredictions();

    if (predictions.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Inga tidigare förutsägelser finns att hämta. Gör en förutsägelse först!");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("==============================================");
        Console.WriteLine("             Tidigare förutsägelser           ");
        Console.WriteLine("==============================================\n");


        foreach (var p in predictions)
        {
            if (p.PredictedDirection == "Upp")
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            // Istället för att alltid visa säkerheten för positiv(uppgång), byts det till hur OSÄKER 
            // modellen är för uppgång, när Direction är "Ned".
            // Visar med andra ord alltid över 50% och hur säker den är på uppgång/nedgång.
            // Gångrar confidence med 100 för att få 1-100%.
            float confidence = p.PredictedDirection == "Upp" ? p.Probability : (1 - p.Probability);

            Console.WriteLine($"{p.Date:g} | {p.Symbol}");
            Console.WriteLine($"Klassificering (uppgång/nedgång): {p.PredictedDirection} | Säkerhet: {confidence * 100:F1}%");
            Console.WriteLine($"Regression (nästa dags stäningskurs): {p.PredictedClose}\n");
        }
    }

    Console.ResetColor();
    Console.Write("\nTryck på valfri tangent för att återgå till menyn.");
    Console.ReadKey(intercept: true);
}
static void ShowPopularStocks()
{
    
}


// Console.WriteLine("\nTränar modeller (detta kan ta upp till en minut)...");
// kod för att köra träning av modeller.
// mlService.TrainAndEvaluate(stockData); 
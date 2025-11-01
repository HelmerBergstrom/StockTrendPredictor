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

    // REGRESSIONSMODELLEN
    // Förutsägning av nästa dags stängningskurs.
    var latestData = stockData.Last(); // Gårdagens data.
    var mlContext = new MLContext();
    var predictionEngine = mlContext.Model
        .CreatePredictionEngine<StockData, StockPrediction>(regressionModel);
    var predictedClose = predictionEngine.Predict(latestData);
    var yesterdaysClose = latestData.Close; // Gårdagens stängning.

    // KLASSIFICERINGSMODELLEN.
    // Förutsägning av nästa dags upp/nedgång.
    var binaryPredictionEngine = mlContext.Model
        .CreatePredictionEngine<StockData, StockDirectionPrediction>(binaryModel);
    var predictedDirection = binaryPredictionEngine.Predict(latestData);

    string direction = predictedDirection.PredictedLabel ? "Upp" : "Ned";

    // confidence visar alltid hur SÄKER modellen är på sitt beslut.
    float confidence = predictedDirection.PredictedLabel ? predictedDirection.Probability : 1 - predictedDirection.Probability;

    // skapar variabel för tom textsträng. Används i if-sats nedan.
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
    Console.WriteLine($"Förväntad {upOrDown} för aktien ({symbol}) till kursen: {predictedClose.PredictedClose:F2}");

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

    // Sparar i JSON-filen.
    var storage = new PredictionStorageService();
    var record = new PredictionRecord
    {
        Symbol = symbol,
        Date = DateTime.Now,
        PredictedClose = predictedClose.PredictedClose,
        PredictedDirection = direction,
        Probability = predictedDirection.Probability,
        PreviousClose = latestData.Close
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
            string regressionDirection = p.PredictedClose > p.PreviousClose ? "Upp" : "Ned";

            // Visar hur SÄKER modellen är på sin förutsägelse. Kommer alltid vara över 50%.
            float confidence = p.PredictedDirection == "Upp" ? p.Probability : (1 - p.Probability);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{p.Date:g} | {p.Symbol}");

            // if-satser för att skriva ut i olika färger beroende på vad som förutsetts.
            if (p.PredictedDirection == "Upp")
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"Klassificeringsmodellen förutspådde: {p.PredictedDirection} | Säkerhet: {confidence * 100:F1}%");

            if (regressionDirection == "Upp")
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }

            Console.WriteLine($"Regressionsmodellen förutspådde: {p.PredictedClose} ({regressionDirection})");
            Console.WriteLine($"Föregående dags stängningskurs var: {p.PreviousClose}\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"===============================================================\n");
        }
    }

    Console.ResetColor();
    Console.Write("\nTryck på valfri tangent för att återgå till menyn.\n");
    Console.ReadKey(intercept: true);
}
static void ShowPopularStocks()
{
    // Skriver ut populära aktier och deras symboler.
    var stocks = new Dictionary<string, string>
    {
        { "AAPL", "Apple" },
        { "MSFT", "Microsoft" },
        { "NVDA", "Nvidia" },
        { "TSLA", "Tesla" },
        { "AMZN", "Amazon.com" },
        { "META", "Meta Platforms" },
        { "ORCL", "Oracle" },
        { "NFLX", "Netflix" },
        { "PLTR", "Palantir Technologies" },
        { "AMD", "Advanced Micro Devices" },
        { "GOOG", "Alphabet" },
        { "TSM", "Taiwan Semiconductor Manufacturing" },
        { "IBM", "International Business Machines" }
    };

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("=======================");
    Console.WriteLine("=   Populära Aktier   =");
    Console.WriteLine("=======================\n");

    // Loopar igenom och skriver ut vardera aktie med Key(symbol) och Value(Bolag).
    foreach (var stock in stocks)
    {
        Console.WriteLine($"{stock.Key} - {stock.Value}");
    }

    Console.ResetColor();
    Console.WriteLine("\nTryck på valfri tangent för att återgå till menyn");
    // intercept: true. Detta gör att det användaren skriver inte syns i konsolen.
    Console.ReadKey(intercept: true);
}


// Console.WriteLine("\nTränar modeller (detta kan ta upp till en minut)...");
// kod för att köra träning av modeller.
// mlService.TrainAndEvaluate(stockData); 
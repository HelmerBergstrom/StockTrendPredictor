using System.Reflection.Emit;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using StockTrendPredictor.Models;

namespace StockTrendPredictor.Services
{
    public class MLService
    {
        private MLContext _mlContext;

        public MLService(int seed = 0)
        {
            _mlContext = new MLContext(seed);
        }

        // Modell för att träna och utvärdera modell.
        public void TrainAndEvaluate(List<StockData> stockData)
        {
            // Förbereder data för att göra förutsägning en dag framåt.
            var shiftedData = new List<StockData>();
            for (int i = 0; i < stockData.Count - 1; i++)
            {
                shiftedData.Add(new StockData
                {
                    Date = stockData[i].Date,
                    Open = stockData[i].Open,
                    High = stockData[i].High,
                    Low = stockData[i].Low,
                    Volume = stockData[i].Volume,
                    Close = stockData[i + 1].Close,
                    WillRise = stockData[i + 1].Close > stockData[i].Close
                });
            }

            IDataView data = _mlContext.Data.LoadFromEnumerable(shiftedData);
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2);


            Console.WriteLine($"Antal dagar: {shiftedData.Count}");
            Console.WriteLine($"Uppgångar (WillRise = true): {shiftedData.Count(d => d.WillRise)}");
            Console.WriteLine($"Nedgångar (WillRise = false): {shiftedData.Count(d => !d.WillRise)}");


            // TRÄNING AV REGRESSION, alltså nästa dags Close.
            Console.WriteLine("Kör AutoML för regression...");
            var regressionSettings = new RegressionExperimentSettings { MaxExperimentTimeInSeconds = 60 };
            var regressionExperiment = _mlContext.Auto().CreateRegressionExperiment(regressionSettings);
            var regressionResult = regressionExperiment.Execute(split.TrainSet, labelColumnName: "Close");

            // Resultat av träningen.
            var bestRegression = regressionResult.BestRun;
            var regressionModel = bestRegression.Model;
            var regressionPredictions = regressionModel.Transform(split.TestSet);
            var regressionMetrics = _mlContext.Regression.Evaluate(regressionPredictions, labelColumnName: "Close");

            // Skriver ut resultat till konsol. Sparar modellen i zip-fil.
            Console.WriteLine($"\n Bästa regressionsmodellen: {bestRegression.TrainerName}");
            Console.WriteLine($"R2: {regressionMetrics.RSquared}  RMSE: {regressionMetrics.RootMeanSquaredError}");
            _mlContext.Model.Save(regressionModel, split.TrainSet.Schema, "bestRegressionModel.zip");

            // Träning av klassificering, uppgång/nedgång.
            Console.WriteLine("\n Kör AutoML för klassificering...");
            var binarySettings = new BinaryExperimentSettings { MaxExperimentTimeInSeconds = 60 };
            var binaryExperiment = _mlContext.Auto().CreateBinaryClassificationExperiment(binarySettings);
            var binaryResult = binaryExperiment.Execute(split.TrainSet, labelColumnName: "WillRise");

            // Resultat av träningen.
            var bestBinary = binaryResult.BestRun;
            var binaryModel = bestBinary.Model;
            var binaryPredictions = binaryModel.Transform(split.TestSet);
            var binaryMetrics = _mlContext.BinaryClassification.Evaluate(binaryPredictions, labelColumnName: "WillRise");

            // Skriver ut resultat till konsol. Sparar modellen i zip-fil.
            Console.WriteLine($"Bästa klassificeringsmodellen: {bestBinary.TrainerName}");
            Console.WriteLine($"Noggranhet(Accuracy): {binaryMetrics.Accuracy:P2}");
            _mlContext.Model.Save(binaryModel, split.TrainSet.Schema, "bestBinaryModel.zip");

            Console.WriteLine("\n Modeller sparade:");
            Console.WriteLine(" - bestRegressionModel.zip (Nästa dags stängning)");
            Console.WriteLine(" - bestBinaryModel.zip (Uppgång/Nedgång)");

        }   
    }
}
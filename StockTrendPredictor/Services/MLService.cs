using System.Reflection.Emit;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
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
                    DailyRange = stockData[i].High - stockData[i].Low,
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
            var regressionSettings = new RegressionExperimentSettings { MaxExperimentTimeInSeconds = 30 };
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
            if (!shiftedData.Any(d => d.WillRise) || !shiftedData.Any(d => !d.WillRise))
            {
                Console.WriteLine("Kan inte träna klassificeringsmodellen. Datan innehåller endast uppgånger eller nedgånar.");
            }
            else
            {
                Console.WriteLine("\n Tränar binärklassificeringsmodell med FastTree...");

                // Kombinerar alla numeriska kolumner till en "Features"-kolumn.
                var featurePipeline = _mlContext.Transforms.Concatenate("Features",
                    new[] { "Open", "High", "Low", "Volume", "DailyRange" })
                    .Append(_mlContext.Transforms.NormalizeMinMax("Features"));

                // Skapa tränaren
                var trainer = _mlContext.BinaryClassification.Trainers
                    .FastTree(labelColumnName: "WillRise", featureColumnName: "Features");

                // Kombinera pipeline + tränare
                var trainingPipeline = featurePipeline.Append(trainer);

                // Träna modellen
                var binaryModel = trainingPipeline.Fit(split.TrainSet);

                var binaryPredictions = binaryModel.Transform(split.TestSet);

                var binaryMetrics = _mlContext.BinaryClassification.Evaluate(binaryPredictions, labelColumnName: "WillRise");

                Console.WriteLine($"Noggrannhet: {binaryMetrics.Accuracy:P2}");

                if (!double.IsNaN(binaryMetrics.AreaUnderRocCurve))
                {
                    Console.WriteLine($"AUC: {binaryMetrics.AreaUnderRocCurve}");
                }
                else
                {
                    Console.WriteLine("AUC kund inte beräknas. Saknar negativ klass i testdata");
                }

                _mlContext.Model.Save(binaryModel, split.TrainSet.Schema, "bestBinaryModel.zip");
            }

            Console.WriteLine("\n Modeller sparade:");
            Console.WriteLine(" - bestRegressionModel.zip (Nästa dags stängning)");
            Console.WriteLine(" - bestBinaryModel.zip (Uppgång/Nedgång)");

        }   
    }
}
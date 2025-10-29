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
            // Laddar data till ML.Net format.
            IDataView data = _mlContext.Data.LoadFromEnumerable(stockData);

            // Delar upp test och träning.
            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 1);

            // Träningstiden till 60sek.
            var settings = new RegressionExperimentSettings
            {
                MaxExperimentTimeInSeconds = 60
            };

            var experiment = _mlContext.Auto().CreateRegressionExperiment(settings);

            Console.WriteLine("Kör AutoML-experiment...");

            // Kör experimentet med label "Close". Stängningskurs med andra ord.
            var result = experiment.Execute(split.TrainSet, labelColumnName: "Close");

            // Visa resultatet.
            var best = result.BestRun;
            Console.WriteLine($"Bästa modellens namn: {best.TrainerName}");

            // Utvärdera på testdata
            var trainedModel = result.BestRun.Model;
            var predictions = trainedModel.Transform(split.TestSet);
            var metrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: "Close");

            // Skriver ut resultat.
            Console.WriteLine($"Testresultat(1 = perfektion): {metrics.RSquared}");

            // Sparar bästa modellen till en zip-fil.
            string modelPath = "bestModel.zip";
            _mlContext.Model.Save(trainedModel, split.TrainSet.Schema, modelPath);
            Console.WriteLine($"Modellen sparad till: {modelPath}");
        }   
    }
}
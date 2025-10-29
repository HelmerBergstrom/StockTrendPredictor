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

        public void TrainAndEvaluate(List<StockData> stockData)
        {
            // Laddar data till ML.Net format.
            IDataView data = _mlContext.Data.LoadFromEnumerable(stockData);

            var split = _mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 1);

            var settings = new RegressionExperimentSettings
            {
                MaxExperimentTimeInSeconds = 30
            };

            var experiment = _mlContext.Auto().CreateRegressionExperiment(settings);
        }   
    }
}
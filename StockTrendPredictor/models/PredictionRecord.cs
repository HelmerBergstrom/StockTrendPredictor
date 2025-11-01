namespace StockTrendPredictor.Models
{
    public class PredictionRecord
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public float PredictedClose { get; set; }
        public string PredictedDirection { get; set; } = string.Empty;
        public float Probability { get; set; }
        public float PreviousClose { get; set; }
    }
}
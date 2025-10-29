namespace StockTrendPredictor.Models
{
    // Data från API:et
    public class StockData
    {
        public DateTime Date { get; set; }
        public float Open { get; set; }
        public float High { get; set; }
        public float Low { get; set; }
        public float Close { get; set; }
        public float Volume { get; set; }
    }

    // Data som skickas till ML.Net
    public class StockInput
    {

    }

    // Data som ges ut efter ML tränats.
    public class StockPrediction
    {

    }
}
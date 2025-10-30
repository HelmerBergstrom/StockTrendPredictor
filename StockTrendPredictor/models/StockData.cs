using Microsoft.ML.Data;

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

        // Förutsäga upp- eller nedgång
        // 1 om morgondagens stängning är högre än dagens, annars 0.
        public bool WillRise { get; set; }
        public float DailyRange { get; set; }
        public float MovingAverage { get; set; }
    }

    // För regression. Förutsägning av nästa dags stängningspris ("Close").
    public class StockPrediction
    {
        [ColumnName("Score")]
        public float PredictedClose { get; set; }
    }

    // Klassifiering, Upp/Ned-förutsägelse.
    public class StockDirectionPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool PredictedLabel { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
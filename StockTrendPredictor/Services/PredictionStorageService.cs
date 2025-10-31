using System.Text.Json;
using StockTrendPredictor.Models;

namespace StockTrendPredictor.Services
{
    public class PredictionStorageService
    {
        private const string FilePath = @"predictions.json";

        public void SavePrediction(PredictionRecord record)
        {
            // Laddar befintliga poster i filen.
            var records = LoadPredictions();
            // Lägger till i filen.
            records.Add(record);

            // Skriver tillbaka hela listan i formaterad JSON.
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(records, options);
            File.WriteAllText(FilePath, json);

        }

        public List<PredictionRecord> LoadPredictions()
        {
            // Kontrollerar om filen existerar. Gör den inte det returneras en tom lista för att inte krascha programmet.
            if (!File.Exists(FilePath))
            {
                return new List<PredictionRecord>();
            }

            // Läser in hela innehållet i filen.
            var json = File.ReadAllText(FilePath);
            // Returnerar omvandlad JSON-text till en lista likt PredictionRecord.
            // Går något fel returneras en tom lista även här.
            return JsonSerializer.Deserialize<List<PredictionRecord>>(json) ?? new List<PredictionRecord>();
        }
    }
}
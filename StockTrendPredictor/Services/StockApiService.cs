using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using StockTrendPredictor.Models;

namespace StockTrendPredictor.Services
{
    public class StockApiService
    {
        private readonly HttpClient client = new();
        private readonly string _apiKey = "4F5TKNYGFANV6O5U";
    }
}
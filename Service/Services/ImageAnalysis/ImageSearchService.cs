using Microsoft.Extensions.Configuration;
using Service.Services.ImageAnalysis.Interfaces;
using System.Net;
using System.Text.Json;

namespace Service.Services.ImageAnalysis
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ImageSearchService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["ImageSearch:ApiKey"] ?? string.Empty;
        }

        public async Task<List<string>> SearchImagesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new List<string> { "https://via.placeholder.com/512?text=No+Image+Found" };

            var url =
                $"https://serpapi.com/search.json?q={WebUtility.UrlEncode(query)}" +
                $"&engine=google_images&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return new List<string> { "https://via.placeholder.com/512?text=No+Image+Found" };

            using var doc = JsonDocument.Parse(json);

            var images = new List<string>();

            if (doc.RootElement.TryGetProperty("images_results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (item.TryGetProperty("original", out var img))
                    {
                        var imageUrl = img.GetString();
                        if (!string.IsNullOrWhiteSpace(imageUrl))
                            images.Add(imageUrl);
                    }
                }
            }

            if (images.Count == 0)
                return new List<string> { "https://via.placeholder.com/512?text=No+Image+Found" };

            return images.Take(3).ToList();
        }
    }
}

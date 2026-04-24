using Microsoft.Extensions.Configuration;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Service.Services
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ImageSearchService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            _apiKey = config["ImageSearch:ApiKey"]
                ?? throw new InvalidOperationException("SerpAPI key missing");
        }

        public async Task<List<string>> SearchImagesAsync(string query)
        {
            var url =
                $"https://serpapi.com/search.json?q={WebUtility.UrlEncode(query)}" +
                $"&engine=google_images&api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Return safe empty list on failure; retry handler will have already attempted transient errors.
                return new List<string>(); 
            }

            using var doc = JsonDocument.Parse(json);

            var images = new List<string>();

            if (doc.RootElement.TryGetProperty("images_results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (item.TryGetProperty("original", out var img))
                    {
                        var urlImg = img.GetString();
                        if (!string.IsNullOrWhiteSpace(urlImg))
                            images.Add(urlImg);
                    }
                }
            }
            if (images.Count == 0)
                return new List<string>
            {
                "https://via.placeholder.com/512?text=No+Image+Found"
            };

            return images.Take(3).ToList();
        }
    }
}

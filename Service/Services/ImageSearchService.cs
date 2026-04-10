using Microsoft.Extensions.Configuration;
using Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Service.Services
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ImageSearchService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Pexels:ApiKey"];
        }

        public async Task<List<string>> SearchPhotosAsync(string query)
        {
            var url = $"https://api.pexels.com/v1/search?query={query}&per_page=5";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", _apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            var photos = doc.RootElement.GetProperty("photos");

            var images = new List<string>();

            foreach (var photo in photos.EnumerateArray())
            {
                var imageUrl = photo.GetProperty("src").GetProperty("medium").GetString();
                images.Add(imageUrl);
            }

            return images;
        }
    
    }
}

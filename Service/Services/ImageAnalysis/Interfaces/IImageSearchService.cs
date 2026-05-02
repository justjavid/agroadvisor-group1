namespace Service.Services.ImageAnalysis.Interfaces
{
    public interface IImageSearchService
    {
        Task<List<string>> SearchImagesAsync(string query);
    }
}
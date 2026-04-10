namespace Service.Services.Interfaces
{
    public interface IImageSearchService
    {
        Task<List<string>> SearchPhotosAsync(string query);
    }
}

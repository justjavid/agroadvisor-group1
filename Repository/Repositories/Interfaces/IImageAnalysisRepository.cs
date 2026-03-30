using System.Threading.Tasks;
using Domain.Models.ImageAnalysis;

namespace Repository.Repositories.Interfaces
{
    public interface IImageAnalysisRepository
    {
        Task AddAsync(AnalysisData entity);
    }
}

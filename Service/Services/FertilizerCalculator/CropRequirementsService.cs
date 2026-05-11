using Domain.Models.FertilizerCalculator;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.Interfaces;

namespace Service.Services;

public class CropRequirementsService : ICropRequirementsService
{
    private readonly FertilizerCalculatorDbContext _db;

    public CropRequirementsService(FertilizerCalculatorDbContext db)
    {
        _db = db;
    }

    public async Task<AddCropRequirementsResponse> AddAsync(AddCropRequirementsRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new CropRequirements
        {
            CropType = request.CropType,
            GrowthStage = request.GrowthStage,
            N = request.N,
            P = request.P,
            K = request.K
        };

        _db.CropRequirements.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new AddCropRequirementsResponse
        {
            Id = entity.Id,
            CropType = entity.CropType,
            GrowthStage = entity.GrowthStage,
            N = entity.N,
            P = entity.P,
            K = entity.K
        };
    }

    public async Task<List<AddCropRequirementsResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CropRequirements
            .AsNoTracking()
            .Select(x => new AddCropRequirementsResponse
            {
                Id = x.Id,
                CropType = x.CropType,
                GrowthStage = x.GrowthStage,
                N = x.N,
                P = x.P,
                K = x.K
            })
            .ToListAsync(cancellationToken);
    }
}

using Domain.Models.FertilizerCalculator;
using Repository.Data;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator.Interfaces;

namespace Service.Services.FertilizerCalculator;

public class SoilMultiplierService : ISoilMultiplierService
{
    private readonly AppDbContext _db;

    public SoilMultiplierService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AddSoilMultiplierResponse> AddAsync(AddSoilMultiplierRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SoilMultiplier
        {
            SoilType = request.SoilType,
            NMultiplier = request.NMultiplier,
            PMultiplier = request.PMultiplier,
            KMultiplier = request.KMultiplier
        };

        _db.SoilMultiplier.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new AddSoilMultiplierResponse
        {
            Id = entity.Id,
            SoilType = entity.SoilType,
            NMultiplier = entity.NMultiplier,
            PMultiplier = entity.PMultiplier,
            KMultiplier = entity.KMultiplier
        };
    }
}
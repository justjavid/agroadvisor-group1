using Domain.Models.FertilizerCalculator;
using Repository.Data;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.Interfaces;

namespace Service.Services;

public class FertilizerService : IFertilizerService
{
    private readonly FertilizerCalculatorDbContext _db;

    public FertilizerService(FertilizerCalculatorDbContext db)
    {
        _db = db;
    }

    public async Task<AddFertilizerResponse> AddAsync(AddFertilizerRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Fertilizer
        {
            Name = request.Name,
            NPct = request.NPct,
            PPct = request.PPct,
            KPct = request.KPct,
            CostPerKg = request.CostPerKg
        };

        _db.Fertilizers.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new AddFertilizerResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            NPct = entity.NPct,
            PPct = entity.PPct,
            KPct = entity.KPct,
            CostPerKg = entity.CostPerKg
        };
    }
}

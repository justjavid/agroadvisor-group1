using Domain.Models.FertilizerCalculator;
using Repository.Data;
using Service.DTOs.FertilizerCalculatorDTOs.Requests;
using Service.DTOs.FertilizerCalculatorDTOs.Responses;
using Service.Services.FertilizerCalculator.Interfaces;

namespace Service.Services.FertilizerCalculator;

public class FertilizerService : IFertilizerService
{
    private readonly AppDbContext _db;

    public FertilizerService(AppDbContext db)
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
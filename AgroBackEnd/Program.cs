using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.Services;
using Service.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<FertilizerCalculatorDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("PostgreSQL")));

builder.Services.AddScoped<IFertilizerService, FertilizerService>();
builder.Services.AddScoped<ISoilMultiplierService, SoilMultiplierService>();
builder.Services.AddScoped<ICropRequirementsService, CropRequirementsService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

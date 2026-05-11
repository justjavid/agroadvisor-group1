using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.Services;
using Service.Services.Interfaces;

try
{
    var envPath = Path.Combine(AppContext.BaseDirectory, ".env");
    if (!File.Exists(envPath))
        envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
        Env.Load(envPath);
}
catch
{
    // No .env file is okay; appsettings/env vars still work.
}

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
builder.Services.AddHttpClient<IFertilizerAiInsightService, FertilizerAiInsightService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped(sp =>
{
    var options = builder.Configuration.GetSection("AiOptions").Get<AiOptions>() ?? new AiOptions();

    // Env vars take precedence over appsettings for safer secret handling.
    options.ApiKey = FirstNonEmpty(
        Environment.GetEnvironmentVariable("AI_API_KEY"),
        Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
        Environment.GetEnvironmentVariable("AiOptions__ApiKey"),
        options.ApiKey);

    options.Model = FirstNonEmpty(
        Environment.GetEnvironmentVariable("AI_MODEL"),
        Environment.GetEnvironmentVariable("GEMINI_MODEL"),
        Environment.GetEnvironmentVariable("AiOptions__Model"),
        options.Model);

    options.Endpoint = FirstNonEmpty(
        Environment.GetEnvironmentVariable("AI_ENDPOINT"),
        Environment.GetEnvironmentVariable("GEMINI_ENDPOINT"),
        Environment.GetEnvironmentVariable("AiOptions__Endpoint"),
        options.Endpoint);

    return options;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

static string FirstNonEmpty(params string?[] values)
{
    foreach (var value in values)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return string.Empty;
}

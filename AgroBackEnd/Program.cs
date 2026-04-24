using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Repository.Data;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Service.Services.Interfaces;
using Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Framework services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gemini API",
        Version = "v1"
    });
});

// Database
var conString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ImageAnalysisDbContext>(options =>
    options.UseSqlServer(conString));

// Repositories and application services
builder.Services.AddScoped<IImageAnalysisRepository, ImageAnalysisRepository>();
// Add a simple retry handler for transient errors and register HttpClients for services
builder.Services.AddTransient<Service.Handlers.SimpleRetryHandler>();
builder.Services.AddHttpClient<IImageAnalysisService, ImageAnalysisService>()
    .AddHttpMessageHandler<Service.Handlers.SimpleRetryHandler>();
builder.Services.AddHttpClient<IImageSearchService, ImageSearchService>()
    .AddHttpMessageHandler<Service.Handlers.SimpleRetryHandler>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gemini API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
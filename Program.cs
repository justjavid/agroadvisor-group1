using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repository.Data;
using Repository.Data.FertilizerCalculator;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Service.Services.Auth;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;
using Service.Services.ImageAnalysis;
using Service.Services.ImageAnalysis.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ImageAnalysisDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IImageAnalysisRepository, ImageAnalysisRepository>();

// Services - Image Analysis
builder.Services.AddHttpClient<IImageAnalysisService, ImageAnalysisService>();
builder.Services.AddHttpClient<IImageSearchService, ImageSearchService>();

// Database - Fertilizer Calculator
builder.Services.AddDbContext<FertilizerCalculatorDbContext>(options =>
    options.UseSqlite("Data Source=fertilizercalculator.db"));

// Services - Fertilizer Calculator
builder.Services.AddScoped<IFertilizerService, FertilizerService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddScoped<ICropRequirementsService, CropRequirementsService>();
builder.Services.AddScoped<ISoilMultiplierService, SoilMultiplierService>();

// Database - Auth
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite("Data Source=auth.db"));

// Auth
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "dev-only-super-secret-key-change-me-please-32chars";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgroAdvisor";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AgroAdvisor";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Auto-create SQLite databases on startup
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ImageAnalysisDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<FertilizerCalculatorDbContext>().Database.EnsureCreated();
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

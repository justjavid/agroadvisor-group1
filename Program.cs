using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Repository.Data;
using Repository.Repositories;
using Repository.Repositories.Interfaces;
using Service.Services.Auth;
using Service.Services.ChatBot;
using Service.Services.ChatBot.Interfaces;
using Service.Services.FertilizerCalculator;
using Service.Services.FertilizerCalculator.Interfaces;
using Service.Services.ImageAnalysis;
using Service.Services.ImageAnalysis.Interfaces;
using System.Text;

try
{
    Env.Load();
}
catch
{
    // No .env file is okay; appsettings/env vars still work.
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste your JWT here (without the 'Bearer ' prefix).",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Id = "Bearer",
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme
        }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Database — single SQL Server context for Auth, Image Analysis, and Fertilizer Calculator
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IImageAnalysisRepository, ImageAnalysisRepository>();

// Services - ChatBot
builder.Services.AddHttpClient<IChatService, ChatService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped(sp =>
{
    var options = builder.Configuration.GetSection("ChatAiOptions").Get<ChatAiOptions>() ?? new ChatAiOptions();

    options.ApiKey = FirstNonEmpty(
        Environment.GetEnvironmentVariable("CHAT_AI_API_KEY"),
        Environment.GetEnvironmentVariable("ChatAiOptions__ApiKey"),
        Environment.GetEnvironmentVariable("AI_API_KEY"),
        Environment.GetEnvironmentVariable("AiOptions__ApiKey"),
        Environment.GetEnvironmentVariable("GEMINI_API_KEY"),
        Environment.GetEnvironmentVariable("GeminiSettings__ApiKey"),
        options.ApiKey);

    options.Model = FirstNonEmpty(
        Environment.GetEnvironmentVariable("CHAT_AI_MODEL"),
        Environment.GetEnvironmentVariable("ChatAiOptions__Model"),
        Environment.GetEnvironmentVariable("AI_MODEL"),
        Environment.GetEnvironmentVariable("AiOptions__Model"),
        Environment.GetEnvironmentVariable("GEMINI_MODEL"),
        options.Model);

    options.Endpoint = FirstNonEmpty(
        Environment.GetEnvironmentVariable("CHAT_AI_ENDPOINT"),
        Environment.GetEnvironmentVariable("ChatAiOptions__Endpoint"),
        Environment.GetEnvironmentVariable("AI_ENDPOINT"),
        Environment.GetEnvironmentVariable("AiOptions__Endpoint"),
        Environment.GetEnvironmentVariable("GEMINI_ENDPOINT"),
        options.Endpoint);

    options.SystemPrompt = FirstNonEmpty(
        Environment.GetEnvironmentVariable("CHAT_AI_SYSTEM_PROMPT"),
        Environment.GetEnvironmentVariable("ChatAiOptions__SystemPrompt"),
        options.SystemPrompt);

    return options;
});

// Services - Image Analysis
builder.Services.AddHttpClient<IImageAnalysisService, ImageAnalysisService>();
builder.Services.AddHttpClient<IImageSearchService, ImageSearchService>();

// Services - Fertilizer Calculator
builder.Services.AddScoped<IFertilizerService, FertilizerService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddScoped<ICropRequirementsService, CropRequirementsService>();
builder.Services.AddScoped<ISoilMultiplierService, SoilMultiplierService>();

// AI insights for the fertilizer calculator (Gemini or OpenAI-compatible)
builder.Services.AddHttpClient<IFertilizerAiInsightService, FertilizerAiInsightService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(20));
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

// Apply pending EF Core migrations on startup.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    // Serve Swagger UI at the site root so "/" shows the API explorer.
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "AgroAdvisor API";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AgroAdvisor API v1");
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    options.DefaultModelsExpandDepth(-1);
    options.EnableDeepLinking();
    options.DisplayRequestDuration();
    options.EnableFilter();
});

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")))
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
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

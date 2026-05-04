using AgroBackEnd.Middlewares;
using Microsoft.EntityFrameworkCore;
using Repository.Data;
using Service.Services;
using Service.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("gemini");

// Services
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddDbContext<ChatBotDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

var app = builder.Build();

// Dev tools
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Middleware
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthorization();

// Endpoints
app.MapControllers();

app.Run();
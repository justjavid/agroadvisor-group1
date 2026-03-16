using Microsoft.EntityFrameworkCore;
using Repository.Data;

var builder = WebApplication.CreateBuilder(args);

// ... rest of your setup
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<FertilizerCalculatorDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("PostgreSQL")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

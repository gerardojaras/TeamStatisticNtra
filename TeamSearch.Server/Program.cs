using Microsoft.EntityFrameworkCore;
using TeamSearch.Application.Repositories;
using TeamSearch.Application.Services;
using TeamSearch.Infrastructure;
using TeamSearch.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=teamsearch.db";
builder.Services.AddDbContext<TeamSearchDbContext>(options =>
{
    options.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
});

builder.Services.AddScoped<ITeamRecordRepository, TeamRecordRepository>();
builder.Services.AddScoped<ITeamRecordService, TeamRecordService>();

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
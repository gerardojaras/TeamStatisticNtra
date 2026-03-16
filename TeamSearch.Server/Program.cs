using Microsoft.EntityFrameworkCore;
using TeamSearch.Application.Repositories;
using TeamSearch.Application.Services;
using TeamSearch.Infrastructure;
using TeamSearch.Infrastructure.Repositories;
using TeamSearch.Infrastructure.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=teamsearch.db";
// Resolve to canonical location under TeamSearch.Server so all tools and runtime use the same DB file
connectionString = SqliteConnectionStringResolver.Resolve(connectionString);
builder.Services.AddDbContext<TeamSearchDbContext>(options =>
{
    options.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
});

builder.Services.AddScoped<ITeamRecordRepository, TeamRecordRepository>();
builder.Services.AddScoped<ITeamRecordService, TeamRecordService>();
// Register IDbContextFactory so background services and other components
// can create DbContext instances without depending on scoped DI.
builder.Services.AddDbContextFactory<TeamSearchDbContext>(options =>
{
    options.UseSqlite(connectionString, sql => sql.MigrationsAssembly("TeamSearch.Infrastructure"));
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapOpenApi();

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.Run();
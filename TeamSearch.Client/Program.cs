using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TeamSearch.Client;
using TeamSearch.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Allow overriding API base URL via configuration (e.g., appsettings or environment)
// If not set and running in Development, default to the common local API URL used by the server project.
string? apiBaseFromConfig = builder.Configuration["ApiBaseUrl"];
Uri httpBase;
if (!string.IsNullOrEmpty(apiBaseFromConfig))
{
    httpBase = new Uri(apiBaseFromConfig);
}
else if (builder.HostEnvironment.IsDevelopment())
{
    // Default developer API URL (adjust if your server uses a different port)
    httpBase = new Uri("https://localhost:7216/");
}
else
{
    httpBase = new Uri(builder.HostEnvironment.BaseAddress);
}

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = httpBase });

// Register the frontend client service that calls the API and provides debounce behavior
builder.Services.AddScoped<ITeamRecordClient, TeamRecordClientService>();

await builder.Build().RunAsync();
using ProductionTracker.Components;
using ProductionTracker.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Configure MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Register all MongoDB services
builder.Services.AddScoped<IProductionService, ProductionService>();
// builder.Services.AddScoped<IStockService, StockService>();
// builder.Services.AddScoped<IAutomationService, AutomationService>();

// Add circuit options for better error handling
builder.Services.Configure<Microsoft.AspNetCore.Components.Server.CircuitOptions>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DetailedErrors = true;
    }
});

// Forwarded headers (Render / reverse proxy) to capture original scheme and IP
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Accept all (Render supplies known proxy); clear defaults
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Data Protection key persistence (prevents antiforgery token invalidation on restarts)
try
{
    var keyPath = Environment.GetEnvironmentVariable("DP_KEYS_PATH") ?? "/app/dpkeys"; // container path
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath))
        .SetApplicationName("BinFlow");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ DataProtection configuration failed: {ex.Message}");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Must run before HTTPS redirection / auth so scheme is correct behind proxy
app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Simple health endpoint for Render health checks
app.MapGet("/health", () => Results.Ok("OK"));

app.Run();
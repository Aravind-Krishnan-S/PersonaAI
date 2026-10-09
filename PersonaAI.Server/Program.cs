using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonaAI.Server;
using PersonaAI.Server.Endpoints;
using PersonaAI.Server.Models;
using CharacterChat.Data;
using CharacterChat.Services;

// ──────────────────────────────────────────────────────────────────────
// PersonaAI Server — Background Windows Service hosting AI personas
// over HTTP for LAN clients (phones, PCs).
// ──────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── Windows Service support ──
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "PersonaAI Server";
});

// ── Load server configuration ──
string configPath = Path.Combine(AppContext.BaseDirectory, "server-config.json");
ServerConfig serverConfig;

if (File.Exists(configPath))
{
    var configJson = File.ReadAllText(configPath);
    serverConfig = JsonSerializer.Deserialize<ServerConfig>(configJson) ?? new ServerConfig();
}
else
{
    serverConfig = new ServerConfig();
}

// Resolve database path: use config or fall back to AppData
if (string.IsNullOrWhiteSpace(serverConfig.DatabasePath))
{
    string appDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PersonaAI");
    Directory.CreateDirectory(appDataPath);
    serverConfig.DatabasePath = Path.Combine(appDataPath, "AppGlobal.db");
}

// ── Configure Kestrel to listen on all interfaces ──
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(serverConfig.Port);
});

// ── Register services ──
builder.Services.AddSingleton(serverConfig);

string connectionString = $"Data Source={serverConfig.DatabasePath}";
builder.Services.AddSingleton(new LlmConfigService(connectionString));
builder.Services.AddSingleton(sp => new CharacterRepository(connectionString));
builder.Services.AddSingleton(sp => new ChunkRepository(connectionString));
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<LlmConfigService>();
    return new ChatService(config);
});
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<LlmConfigService>();
    return new EmbeddingService(config);
});
builder.Services.AddSingleton<VectorSearchService>();

// ── CORS for browser-based clients ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("LanAccess", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ── Discovery responder (UDP broadcast) ──
builder.Services.AddHostedService<DiscoveryResponderService>();

// ── Track server metrics ──
builder.Services.AddSingleton<ServerMetrics>();

var app = builder.Build();

// ── Middleware ──
app.UseCors("LanAccess");

// ── Map API endpoints ──
HealthEndpoints.Map(app);
PersonaEndpoints.Map(app);
ChatEndpoints.Map(app);

// ── Start ──
Console.WriteLine($"╔══════════════════════════════════════════════════╗");
Console.WriteLine($"║  PersonaAI Server v1.0.0                        ║");
Console.WriteLine($"║  Listening on http://0.0.0.0:{serverConfig.Port,-5}               ║");
Console.WriteLine($"║  Database: {Path.GetFileName(serverConfig.DatabasePath),-38}║");
Console.WriteLine($"╚══════════════════════════════════════════════════╝");

app.Run();

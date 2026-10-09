using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using CharacterChat.Data;
using CharacterChat.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using PersonaAI.Server.Models;

namespace PersonaAI.Server.Endpoints
{
    /// <summary>
    /// Health and status endpoints for server monitoring and discovery.
    /// </summary>
    public static class HealthEndpoints
    {
        public static void Map(WebApplication app)
        {
            app.MapGet("/api/health", (ServerConfig config, ServerMetrics metrics, CharacterRepository repo) =>
            {
                int personaCount = 0;
                try { personaCount = repo.GetAllCharacters().Count; } catch { }

                return Results.Ok(new CharacterChat.Models.ServerStatus
                {
                    Status = "online",
                    Hostname = Environment.MachineName,
                    IpAddress = GetLocalIp(),
                    Port = config.Port,
                    Uptime = metrics.GetUptimeDisplay(),
                    UptimeSeconds = metrics.Uptime.TotalSeconds,
                    Version = "1.0.0",
                    ConnectedClients = metrics.ActiveConnections,
                    AvailablePersonas = personaCount,
                    TotalRequests = metrics.TotalRequests,
                    MemoryUsageMb = Math.Round(Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0), 1)
                });
            })
            .WithName("GetHealth")
            ;

            app.MapGet("/api/server/status", (ServerConfig config, ServerMetrics metrics) =>
            {
                return Results.Ok(new
                {
                    port = config.Port,
                    maxClients = config.MaxClients,
                    activeConnections = metrics.ActiveConnections,
                    totalRequests = metrics.TotalRequests,
                    uptime = metrics.GetUptimeDisplay(),
                    memoryMb = Math.Round(Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0), 1),
                    hostname = Environment.MachineName,
                    ip = GetLocalIp()
                });
            })
            .WithName("GetServerStatus")
            ;
        }

        private static string GetLocalIp()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Connect("8.8.8.8", 80);
                if (socket.LocalEndPoint is IPEndPoint ep)
                    return ep.Address.ToString();
            }
            catch { }
            return "127.0.0.1";
        }
    }
}

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CharacterChat.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonaAI.Server.Models;

namespace PersonaAI.Server
{
    /// <summary>
    /// Listens for UDP broadcast discovery requests from clients on the LAN
    /// and responds with this server's identity.
    /// </summary>
    public class DiscoveryResponderService : BackgroundService
    {
        private const int BroadcastPort = 5143;
        private const string DiscoveryMagic = "PERSONAAI_DISCOVER";
        private const string ResponseMagic = "PERSONAAI_HERE";

        private readonly ServerConfig _config;
        private readonly CharacterRepository _characterRepo;
        private readonly ILogger<DiscoveryResponderService> _logger;

        public DiscoveryResponderService(
            ServerConfig config,
            CharacterRepository characterRepo,
            ILogger<DiscoveryResponderService> logger)
        {
            _config = config;
            _characterRepo = characterRepo;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Discovery responder listening on UDP port {Port}", BroadcastPort);

            using var udp = new UdpClient(BroadcastPort);
            udp.EnableBroadcast = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync(stoppingToken);
                    string message = Encoding.UTF8.GetString(result.Buffer);

                    if (message == DiscoveryMagic)
                    {
                        int personaCount = 0;
                        try { personaCount = _characterRepo.GetAllCharacters().Count; } catch { }

                        string hostname = Environment.MachineName;
                        string response = $"{ResponseMagic}|{_config.Port}|{personaCount}|{hostname}";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);

                        await udp.SendAsync(responseBytes, responseBytes.Length, result.RemoteEndPoint);
                        _logger.LogInformation("Discovery response sent to {Endpoint}", result.RemoteEndPoint);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Discovery responder error");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}

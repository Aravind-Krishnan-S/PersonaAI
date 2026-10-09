using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CharacterChat.Models;

namespace CharacterChat.Services
{
    /// <summary>
    /// Discovers PersonaAI servers on the local network via UDP broadcast
    /// and HTTP health-check probing.
    /// </summary>
    public class ServerDiscoveryService
    {
        private const int BroadcastPort = 5143;
        private const string DiscoveryMagic = "PERSONAAI_DISCOVER";
        private const string ResponseMagic = "PERSONAAI_HERE";

        private readonly HttpClient _httpClient;

        public ServerDiscoveryService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        }

        /// <summary>
        /// Sends a UDP broadcast and collects responses from PersonaAI servers on the LAN.
        /// Falls back to subnet scanning if no broadcast responses arrive.
        /// </summary>
        public async Task<List<DiscoveredServer>> ScanNetworkAsync(CancellationToken ct = default)
        {
            var found = new List<DiscoveredServer>();

            // Attempt 1: UDP broadcast
            try
            {
                found = await BroadcastDiscoverAsync(ct);
            }
            catch
            {
                // Broadcast may be blocked; fall through to probe
            }

            // Attempt 2: Probe common subnet IPs on default port if broadcast found nothing
            if (found.Count == 0)
            {
                found = await ProbeSubnetAsync(ct);
            }

            return found;
        }

        /// <summary>
        /// Tests connectivity to a specific PersonaAI server address.
        /// Returns the server status or null if unreachable.
        /// </summary>
        public async Task<ServerStatus?> TestConnectionAsync(string address)
        {
            try
            {
                string url = address.TrimEnd('/') + "/api/health";
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ServerStatus>(json);
                }
            }
            catch
            {
                // Connection failed
            }
            return null;
        }

        /// <summary>
        /// Gets the primary LAN IP address of this machine.
        /// </summary>
        public static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                // Connect to a public DNS to determine the preferred outbound IP
                socket.Connect("8.8.8.8", 80);
                if (socket.LocalEndPoint is IPEndPoint endPoint)
                    return endPoint.Address.ToString();
            }
            catch { }

            return "127.0.0.1";
        }

        private async Task<List<DiscoveredServer>> BroadcastDiscoverAsync(CancellationToken ct)
        {
            var results = new List<DiscoveredServer>();

            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = 2000;

            var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
            byte[] data = Encoding.UTF8.GetBytes(DiscoveryMagic);
            await udp.SendAsync(data, data.Length, broadcastEndpoint);

            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
            {
                try
                {
                    var receiveTask = udp.ReceiveAsync();
                    var delayTask = Task.Delay(500, ct);
                    var completed = await Task.WhenAny(receiveTask, delayTask);

                    if (completed == receiveTask)
                    {
                        var result = await receiveTask;
                        string response = Encoding.UTF8.GetString(result.Buffer);
                        if (response.StartsWith(ResponseMagic))
                        {
                            // Response format: PERSONAAI_HERE|port|personaCount|hostname
                            var parts = response.Split('|');
                            if (parts.Length >= 4)
                            {
                                results.Add(new DiscoveredServer
                                {
                                    IpAddress = result.RemoteEndPoint.Address.ToString(),
                                    Port = int.TryParse(parts[1], out var p) ? p : 5142,
                                    PersonaCount = int.TryParse(parts[2], out var c) ? c : 0,
                                    Hostname = parts[3]
                                });
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    break; // Timeout or error
                }
            }

            return results;
        }

        private async Task<List<DiscoveredServer>> ProbeSubnetAsync(CancellationToken ct)
        {
            var results = new List<DiscoveredServer>();
            string localIp = GetLocalIpAddress();
            if (localIp == "127.0.0.1") return results;

            // Extract subnet prefix (e.g., "192.168.1.")
            var parts = localIp.Split('.');
            if (parts.Length != 4) return results;
            string prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

            // Probe .1 to .254 in parallel (fast timeout)
            var probeTasks = new List<Task>();
            var semaphore = new SemaphoreSlim(20); // Max 20 concurrent probes

            for (int i = 1; i <= 254; i++)
            {
                if (ct.IsCancellationRequested) break;
                string ip = prefix + i;
                if (ip == localIp) continue; // Skip self

                probeTasks.Add(ProbeHostAsync(ip, 5142, results, semaphore, ct));
            }

            await Task.WhenAll(probeTasks);
            return results;
        }

        private async Task ProbeHostAsync(string ip, int port, List<DiscoveredServer> results,
            SemaphoreSlim semaphore, CancellationToken ct)
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var status = await TestConnectionAsync($"http://{ip}:{port}");
                if (status != null && status.Status == "online")
                {
                    lock (results)
                    {
                        results.Add(new DiscoveredServer
                        {
                            IpAddress = ip,
                            Port = port,
                            Hostname = status.Hostname,
                            PersonaCount = status.AvailablePersonas
                        });
                    }
                }
            }
            catch { }
            finally
            {
                semaphore.Release();
            }
        }
    }
}

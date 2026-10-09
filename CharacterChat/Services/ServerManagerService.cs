using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Text.Json;
using System.Threading.Tasks;

namespace CharacterChat.Services
{
    /// <summary>
    /// Manages the PersonaAI Windows Service lifecycle from the WinUI app.
    /// Handles installation, start, stop, and status monitoring.
    /// </summary>
    public class ServerManagerService : IDisposable
    {
        private const string ServiceName = "PersonaAI Server";
        private const string ServiceExeName = "PersonaAI.Server.exe";

        private readonly HttpClient _httpClient;
        private string _serverExePath = "";
        private int _port = 5142;

        public ServerManagerService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            // Resolve the server executable path (sibling project output)
            string basePath = AppContext.BaseDirectory;
            string? solutionDir = FindSolutionDirectory(basePath);
            if (solutionDir != null)
            {
                _serverExePath = Path.Combine(solutionDir, "PersonaAI.Server", "bin", "Debug", "net8.0", ServiceExeName);
            }
        }

        /// <summary>
        /// Checks if the PersonaAI Server Windows Service is installed.
        /// </summary>
        public bool IsServiceInstalled()
        {
            try
            {
                var services = ServiceController.GetServices();
                foreach (var svc in services)
                {
                    if (svc.ServiceName == ServiceName || svc.DisplayName == ServiceName)
                    {
                        svc.Dispose();
                        return true;
                    }
                    svc.Dispose();
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Checks if the server is currently running (either as service or process).
        /// </summary>
        public bool IsServerRunning()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                return sc.Status == ServiceControllerStatus.Running;
            }
            catch
            {
                // Service not installed; check if running as a process
                var procs = Process.GetProcessesByName("PersonaAI.Server");
                return procs.Length > 0;
            }
        }

        /// <summary>
        /// Gets the current server health status from the HTTP endpoint.
        /// </summary>
        public async Task<CharacterChat.Models.ServerStatus?> GetServerStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:{_port}/api/health");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CharacterChat.Models.ServerStatus>(json);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Installs the PersonaAI Server as a Windows Service.
        /// Requires admin privileges.
        /// </summary>
        public async Task<(bool Success, string Message)> InstallServiceAsync()
        {
            if (!File.Exists(_serverExePath))
            {
                return (false, $"Server executable not found at:\n{_serverExePath}\nBuild the PersonaAI.Server project first.");
            }

            try
            {
                // Use sc.exe to create the service
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create \"{ServiceName}\" binPath= \"\\\"{_serverExePath}\\\"\" start= demand DisplayName= \"PersonaAI Server\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0)
                    {
                        // Set description
                        var descPsi = new ProcessStartInfo
                        {
                            FileName = "sc.exe",
                            Arguments = $"description \"{ServiceName}\" \"Hosts PersonaAI personas over HTTP for LAN clients\"",
                            Verb = "runas",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };
                        Process.Start(descPsi)?.WaitForExit();

                        // Add firewall rule
                        await AddFirewallRuleAsync();

                        return (true, "Service installed successfully.");
                    }
                }
                return (false, "Failed to install service. Ensure you have admin rights.");
            }
            catch (Exception ex)
            {
                return (false, $"Installation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the PersonaAI Server.
        /// If installed as a service, starts the service.
        /// Otherwise, launches the executable directly as a background process.
        /// </summary>
        public async Task<(bool Success, string Message)> StartServerAsync()
        {
            try
            {
                if (IsServiceInstalled())
                {
                    using var sc = new ServiceController(ServiceName);
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                    }
                    return (true, "Server service started.");
                }
                else
                {
                    // Run as a background process if service not installed
                    if (!File.Exists(_serverExePath))
                        return (false, "Server executable not found. Build PersonaAI.Server first.");

                    var psi = new ProcessStartInfo
                    {
                        FileName = _serverExePath,
                        WorkingDirectory = Path.GetDirectoryName(_serverExePath),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(psi);

                    // Wait a moment for startup
                    await Task.Delay(2000);
                    return (true, "Server process started.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Start error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the PersonaAI Server. 
        /// </summary>
        public async Task<(bool Success, string Message)> StopServerAsync()
        {
            try
            {
                if (IsServiceInstalled())
                {
                    using var sc = new ServiceController(ServiceName);
                    if (sc.Status == ServiceControllerStatus.Running)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                    }
                    return (true, "Server service stopped.");
                }
                else
                {
                    // Kill the process
                    foreach (var proc in Process.GetProcessesByName("PersonaAI.Server"))
                    {
                        proc.Kill();
                        await proc.WaitForExitAsync();
                        proc.Dispose();
                    }
                    return (true, "Server process stopped.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Stop error: {ex.Message}");
            }
        }

        /// <summary>
        /// Uninstalls the Windows Service.
        /// </summary>
        public async Task<(bool Success, string Message)> UninstallServiceAsync()
        {
            try
            {
                await StopServerAsync();

                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete \"{ServiceName}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                if (proc != null)
                {
                    await proc.WaitForExitAsync();
                    if (proc.ExitCode == 0)
                        return (true, "Service uninstalled successfully.");
                }
                return (false, "Failed to uninstall service.");
            }
            catch (Exception ex)
            {
                return (false, $"Uninstall error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the configured port number.
        /// </summary>
        public void SetPort(int port) => _port = port;

        /// <summary>
        /// Writes the server configuration file so the service can read it.
        /// </summary>
        public void SaveServerConfig(int port, string dbPath, bool autoStart)
        {
            _port = port;
            string configDir = Path.GetDirectoryName(_serverExePath) ?? AppContext.BaseDirectory;
            string configPath = Path.Combine(configDir, "server-config.json");

            var config = new
            {
                port = port,
                maxClients = 10,
                autoStart = autoStart,
                databasePath = dbPath
            };

            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }

        private async Task AddFirewallRuleAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"PersonaAI Server\" dir=in action=allow protocol=TCP localport={_port}",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);
                if (proc != null) await proc.WaitForExitAsync();
            }
            catch { }
        }

        private static string? FindSolutionDirectory(string startPath)
        {
            var dir = new DirectoryInfo(startPath);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CharacterChat.sln")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}

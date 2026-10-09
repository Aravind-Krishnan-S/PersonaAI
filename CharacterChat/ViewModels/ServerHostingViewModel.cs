using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharacterChat.Services;
using CharacterChat.Models;

namespace CharacterChat.ViewModels
{
    public partial class ServerHostingViewModel : ViewModelBase
    {
        private readonly ServerManagerService _serverManager;
        private Microsoft.UI.Dispatching.DispatcherQueue? _dispatcher;
        private System.Threading.Timer? _statusTimer;

        // ── Status Properties ──
        [ObservableProperty] private bool _isServerRunning;
        [ObservableProperty] private bool _isServiceInstalled;
        [ObservableProperty] private string _serverStatus = "Offline";
        [ObservableProperty] private string _serverAddress = "—";
        [ObservableProperty] private string _uptime = "—";
        [ObservableProperty] private int _connectedClients;
        [ObservableProperty] private int _availablePersonas;
        [ObservableProperty] private long _totalRequests;
        [ObservableProperty] private double _memoryUsageMb;
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _isLoading;

        // ── Configuration Properties ──
        [ObservableProperty] private double _serverPort = 5142;
        [ObservableProperty] private bool _autoStart;

        public ServerHostingViewModel()
        {
            _serverManager = new ServerManagerService();
            RefreshStatus();
        }

        /// <summary>
        /// Sets the UI thread dispatcher for timer callbacks.
        /// </summary>
        public void SetDispatcher(Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
            
            // Start polling server status every 3 seconds
            _statusTimer = new System.Threading.Timer(async _ =>
            {
                await PollStatusAsync();
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3));
        }

        /// <summary>
        /// Toggles the server on/off.
        /// </summary>
        [RelayCommand]
        public async Task ToggleServerAsync()
        {
            IsLoading = true;
            StatusMessage = "";

            try
            {
                if (IsServerRunning)
                {
                    var result = await _serverManager.StopServerAsync();
                    StatusMessage = result.Message;
                }
                else
                {
                    _serverManager.SetPort((int)ServerPort);
                    var result = await _serverManager.StartServerAsync();
                    StatusMessage = result.Message;
                }

                // Wait for state change
                await Task.Delay(1500);
                RefreshStatus();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Installs the server as a Windows Service (requires admin).
        /// </summary>
        [RelayCommand]
        public async Task InstallServiceAsync()
        {
            IsLoading = true;
            StatusMessage = "";

            try
            {
                var result = await _serverManager.InstallServiceAsync();
                StatusMessage = result.Message;
                RefreshStatus();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Install error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Uninstalls the Windows Service.
        /// </summary>
        [RelayCommand]
        public async Task UninstallServiceAsync()
        {
            IsLoading = true;
            try
            {
                var result = await _serverManager.UninstallServiceAsync();
                StatusMessage = result.Message;
                RefreshStatus();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Uninstall error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Manually refresh the server status.
        /// </summary>
        [RelayCommand]
        public void RefreshStatus()
        {
            IsServiceInstalled = _serverManager.IsServiceInstalled();
            IsServerRunning = _serverManager.IsServerRunning();
            ServerStatus = IsServerRunning ? "Online" : "Offline";
            
            if (IsServerRunning)
            {
                string ip = ServerDiscoveryService.GetLocalIpAddress();
                ServerAddress = $"http://{ip}:{ServerPort}";
            }
            else
            {
                ServerAddress = "—";
                Uptime = "—";
                ConnectedClients = 0;
                AvailablePersonas = 0;
                TotalRequests = 0;
                MemoryUsageMb = 0;
            }
        }

        private async Task PollStatusAsync()
        {
            if (!IsServerRunning) return;

            try
            {
                var status = await _serverManager.GetServerStatusAsync();
                if (status != null)
                {
                    _dispatcher?.TryEnqueue(() =>
                    {
                        ServerStatus = "Online";
                        IsServerRunning = true;
                        Uptime = status.Uptime;
                        ConnectedClients = status.ConnectedClients;
                        AvailablePersonas = status.AvailablePersonas;
                        TotalRequests = status.TotalRequests;
                        MemoryUsageMb = status.MemoryUsageMb;
                        ServerAddress = $"http://{status.IpAddress}:{status.Port}";
                    });
                }
                else
                {
                    _dispatcher?.TryEnqueue(() =>
                    {
                        // Server may have gone down
                        if (!_serverManager.IsServerRunning())
                        {
                            IsServerRunning = false;
                            ServerStatus = "Offline";
                        }
                    });
                }
            }
            catch { }
        }

        /// <summary>
        /// Cleanup timer on disposal.
        /// </summary>
        public void Cleanup()
        {
            _statusTimer?.Dispose();
            _serverManager.Dispose();
        }
    }
}

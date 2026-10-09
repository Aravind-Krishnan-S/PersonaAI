using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CharacterChat.ViewModels;

namespace CharacterChat.Views
{
    public sealed partial class ServerHostingPage : Page
    {
        public ServerHostingViewModel ViewModel { get; }

        public ServerHostingPage()
        {
            ViewModel = new ServerHostingViewModel();
            this.InitializeComponent();

            // Set dispatcher for UI thread timer callbacks
            ViewModel.SetDispatcher(this.DispatcherQueue);
        }

        private async void ServerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggle)
            {
                // Only act if the toggle state differs from the ViewModel state
                // (avoid recursive triggers from binding updates)
                if (toggle.IsOn != ViewModel.IsServerRunning)
                {
                    await ViewModel.ToggleServerAsync();
                }
            }
        }

        public Windows.UI.Color GetStatusColor(bool running) => running ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red;
        public Microsoft.UI.Xaml.Visibility GetVisibility(string msg) => string.IsNullOrEmpty(msg) ? Visibility.Collapsed : Visibility.Visible;
        public string GetServiceStatusText(bool installed) => installed ? "Installed" : "Not Installed";
    }
}

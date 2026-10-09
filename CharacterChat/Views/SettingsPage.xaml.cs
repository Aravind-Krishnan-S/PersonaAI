using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CharacterChat.ViewModels;
using CharacterChat.Models;

namespace CharacterChat.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            ViewModel = new SettingsViewModel();
            this.InitializeComponent();
            // Password is not a DependencyProperty, so set it manually
            ApiKeyBox.Password = ViewModel.ApiKey ?? string.Empty;
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                ViewModel.ApiKey = pb.Password;
            }
        }

        private void DiscoveredServer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list && list.SelectedItem is DiscoveredServer server)
            {
                ViewModel.ServerAddress = $"http://{server.IpAddress}:{server.Port}";
            }
        }
    }
}

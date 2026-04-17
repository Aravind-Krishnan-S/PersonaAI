using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CharacterChat.ViewModels;

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
    }
}

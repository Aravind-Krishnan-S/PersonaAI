using CharacterChat.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CharacterChat.Views
{
    public sealed partial class LibraryPage : Page
    {
        public LibraryViewModel ViewModel { get; }

        public LibraryPage()
        {
            ViewModel = new LibraryViewModel();
            this.InitializeComponent();
        }

        public Microsoft.UI.Xaml.Visibility GetVisibility(bool isVisible) => isVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        private void StartChat_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Models.Character character)
            {
                if (ViewModel.StartChatCommand.CanExecute(character))
                {
                    ViewModel.StartChatCommand.Execute(character);
                }
            }
        }
    }
}

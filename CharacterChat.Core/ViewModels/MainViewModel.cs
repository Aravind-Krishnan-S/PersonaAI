using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace CharacterChat.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _title = "PersonaAI";

        [ObservableProperty]
        private object? _currentContent;

        public void Navigate(string target)
        {
            // Shell navigation logic handled in Code-Behind usually, 
            // but ViewModel can track current state.
        }
    }
}

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CharacterChat.ViewModels;
using CharacterChat.Data;
using CharacterChat.Models;
using System;
using System.Collections.Generic;

namespace CharacterChat.Views
{
    public sealed partial class ChatPage : Page
    {
        public ChatViewModel ViewModel { get; private set; }

        public ChatPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Dictionary<string, object> dict &&
                dict.TryGetValue("Character", out var characterObj) && characterObj is Character character &&
                dict.TryGetValue("ConnectionString", out var connStrObj) && connStrObj is string connStr)
            {
                ViewModel = new ChatViewModel(character, connStr);
            }
            else
            {
                var placeholderChar = new Character 
                { 
                    Id = 1, 
                    BookId = 1, 
                    Name = "The Narrator", 
                    ProfileJson = "{}" 
                };
                ViewModel = new ChatViewModel(placeholderChar, DatabaseService.GetConnectionString("AppGlobal"));
            }
            // Bind the new ViewModel to the view (it will update x:Bind if we use Bindings.Update())
            Bindings.Update();
        }
    }
}

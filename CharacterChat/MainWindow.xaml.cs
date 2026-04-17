using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CharacterChat;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(null); 
        
        // Initial navigation
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
    }

    private void RootNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(CharacterChat.Views.SettingsPage));
        }
        else if (args.SelectedItemContainer != null)
        {
            var tag = args.SelectedItemContainer.Tag.ToString();
            switch (tag)
            {
                case "Chat":
                    ContentFrame.Navigate(typeof(CharacterChat.Views.ChatPage));
                    break;
                case "Library":
                    ContentFrame.Navigate(typeof(CharacterChat.Views.LibraryPage));
                    break;
            }
        }
    }

    internal void NavigateToChat(object parameter)
    {
        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0]; // Chat is at index 0
        ContentFrame.Navigate(typeof(CharacterChat.Views.ChatPage), parameter);
    }
}

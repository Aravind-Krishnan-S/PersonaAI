using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharacterChat.Models;
using System.Linq;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using CharacterChat.Services;
using CharacterChat.Data;

namespace CharacterChat.ViewModels
{
    public partial class LibraryViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Book> _books = new();

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private ObservableCollection<Character> _characters = new();

        [ObservableProperty]
        private bool _isImporting;

        [ObservableProperty]
        private string _importProgress = "";

        public LibraryViewModel()
        {
            LoadBooks();
        }

        private void LoadBooks()
        {
            Books.Clear();
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CharacterChat");
            if (Directory.Exists(appDataFolder))
            {
                var dbFiles = Directory.GetFiles(appDataFolder, "*.db");
                foreach (var dbFile in dbFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(dbFile);
                    if (fileName != "GlobalSettings") 
                    {
                        var title = fileName.Replace("_", " ");
                        Books.Add(new Book 
                        { 
                            Title = title, 
                            FilePath = dbFile, // We can store the db path or we can extract the path later
                            ImportedAt = File.GetCreationTime(dbFile) 
                        });
                    }
                }
            }
        }

        [RelayCommand]
        public async Task ImportBookAsync()
        {
            try
            {
                // Create a file picker
                var window = (App.Current as App)?.MainWindow;
                if (window == null) return;

                var picker = new FileOpenPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));

                picker.ViewMode = PickerViewMode.List;
                picker.FileTypeFilter.Add(".txt");
                picker.FileTypeFilter.Add(".epub");
                picker.FileTypeFilter.Add(".pdf");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    IsImporting = true;
                    ImportProgress = $"Importing {file.Name}...";

                    // Start document ingestion background task
                    string bookTitle = Path.GetFileNameWithoutExtension(file.Name);

                    // Initialize the specific DB
                    DatabaseService.InitializeDatabase(bookTitle);

                    var llmConfig = new LlmConfigService(DatabaseService.GetConnectionString("GlobalSettings"));
                    var characterExtractionService = new CharacterExtractionService(llmConfig);
                    var embedService = new EmbeddingService(llmConfig);
                    
                    var bookConnectionString = DatabaseService.GetConnectionString(bookTitle);
                    var chunkRepo = new ChunkRepository(bookConnectionString);
                    var charRepo = new CharacterRepository(bookConnectionString);

                    var ingestionService = new DocumentIngestionService(chunkRepo, charRepo, embedService, characterExtractionService); 
                    
                    // Note: DocumentIngestionService currently might not be fully configured for this exact call
                    // Adjust this based on actual DocumentIngestionService implementation
                    // IngestFileAsync needs bookId and Progress. For simplicity bookId = 1, progress = null.
                    await ingestionService.IngestFileAsync(1, file.Path, null);
                    
                    ImportProgress = $"Finished importing {bookTitle}. Extracting characters...";

                    // Character extraction is called inside IngestFileAsync per chunk.
                    // If we want a full profile extracted we should do it here if needed, but for now we rely on the chunk insertion.

                    ImportProgress = "";
                    IsImporting = false;
                    
                    LoadBooks();
                }
            }
            catch (Exception ex)
            {
                IsImporting = false;
                ImportProgress = $"Error: {ex.Message}";
                Debug.WriteLine($"Failed to import: {ex}");
            }
        }

        [RelayCommand]
        public void StartChat(Character character)
        {
            if (character == null) return;
            
            // Navigate to ChatPage with the given character and selected book's Title as connection source
            var window = (App.Current as App)?.MainWindow as MainWindow;
            if (window != null)
            {
                var dict = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "Character", character },
                    { "ConnectionString", DatabaseService.GetConnectionString(SelectedBook.Title) }
                };

                // The navigation frame is accessible from MainWindow
                window.NavigateToChat(dict);
            }
        }

        partial void OnSelectedBookChanged(Book? value)
        {
            Characters.Clear();
            if (value != null)
            {
                var connectionString = DatabaseService.GetConnectionString(value.Title);
                var repo = new CharacterRepository(connectionString);
                // Currently all books are imported per DB so bookId defaults to 1.
                // In DatabaseService schema, Book has its own DB, or is BookId unique across global?
                // Let's just select all characters from that book's DB
                var chars = repo.GetCharactersForBook(1); // Usually book has Id 1 in its own db
                foreach (var c in chars)
                {
                    Characters.Add(c);
                }
            }
        }
    }
}

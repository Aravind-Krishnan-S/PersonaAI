using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharacterChat.Models;
using System.Linq;
using System.Diagnostics;
using CharacterChat.Services;
using CharacterChat.Data;

namespace CharacterChat.ViewModels
{
    public partial class LibraryViewModel : ViewModelBase
    {
        private readonly IFilePickerService _filePickerService;
        private readonly INavigationService _navigationService;

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

        public LibraryViewModel(IFilePickerService filePickerService, INavigationService navigationService)
        {
            _filePickerService = filePickerService;
            _navigationService = navigationService;
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
                            FilePath = dbFile, 
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
                var filePath = await _filePickerService.PickDocumentFileAsync();
                if (!string.IsNullOrEmpty(filePath))
                {
                    IsImporting = true;
                    ImportProgress = $"Importing {Path.GetFileName(filePath)}...";

                    string bookTitle = Path.GetFileNameWithoutExtension(filePath);

                    // Initialize the specific DB
                    DatabaseService.InitializeDatabase(bookTitle);

                    var llmConfig = new LlmConfigService(DatabaseService.GetConnectionString("GlobalSettings"));
                    var characterExtractionService = new CharacterExtractionService(llmConfig);
                    var embedService = new EmbeddingService(llmConfig);
                    
                    var bookConnectionString = DatabaseService.GetConnectionString(bookTitle);
                    var chunkRepo = new ChunkRepository(bookConnectionString);
                    var charRepo = new CharacterRepository(bookConnectionString);

                    var ingestionService = new DocumentIngestionService(chunkRepo, charRepo, embedService, characterExtractionService); 
                    
                    await ingestionService.IngestFileAsync(1, filePath, null);
                    
                    ImportProgress = $"Finished importing {bookTitle}. Extracting characters...";

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
            if (character == null || SelectedBook == null) return;
            _navigationService.NavigateToChat(character, DatabaseService.GetConnectionString(SelectedBook.Title));
        }

        partial void OnSelectedBookChanged(Book? value)
        {
            Characters.Clear();
            if (value != null)
            {
                var connectionString = DatabaseService.GetConnectionString(value.Title);
                var repo = new CharacterRepository(connectionString);
                var chars = repo.GetCharactersForBook(1); 
                foreach (var c in chars)
                {
                    Characters.Add(c);
                }
            }
        }
    }
}

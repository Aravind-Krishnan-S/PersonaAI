using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharacterChat.Models;
using CharacterChat.Services;
using CharacterChat.Data;
using System.Collections.Generic;

namespace CharacterChat.ViewModels
{
    public partial class ChatViewModel : ViewModelBase
    {
        private readonly ChatService _chatService;
        private readonly EmbeddingService _embedService;
        private readonly VectorSearchService _searchService;
        private readonly ChatHistoryRepository _historyRepo;
        private readonly ChunkRepository _chunkRepo;

        private Character _character;
        private List<TextChunk> _allChunks;

        [ObservableProperty] private string _userInput = "";
        [ObservableProperty] private bool _isTyping;
        [ObservableProperty] private string _characterName;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatViewModel(Character character, string dbConnection)
        {
            _character = character;
            _characterName = character.Name;

            var config = new LlmConfigService(dbConnection);
            _chatService = new ChatService(config);
            _embedService = new EmbeddingService(config);
            _searchService = new VectorSearchService();
            _historyRepo = new ChatHistoryRepository(dbConnection);
            _chunkRepo = new ChunkRepository(dbConnection);

            // Load history
            var history = _historyRepo.GetRecentMessages(character.Id, 20);
            foreach (var msg in history) Messages.Add(msg);

            // Load all chunks for RAG (Usually optimized with a vector DB, but here we do in-memory scan for small books)
            _allChunks = _chunkRepo.GetAllChunks(character.BookId);
        }

        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(UserInput)) return;

            string text = UserInput;
            UserInput = "";

            var userMsg = new ChatMessage
            {
                CharacterId = _character.Id,
                Role = "user",
                Content = text,
                Timestamp = DateTime.Now
            };

            Messages.Add(userMsg);
            _historyRepo.SaveMessage(userMsg);

            IsTyping = true;
            
            try
            {
                // 1. Get embedding for the user query
                float[] queryVector = await _embedService.GetEmbeddingAsync(text);

                // 2. Perform RAG (Strict character filtering happens inside SearchForCharacterContext)
                var context = _searchService.SearchForCharacterContext(_allChunks, queryVector, _character.Name);

                // 3. Build System Prompt
                string systemPrompt = SystemPromptBuilder.BuildPrompt(_character, context);

                // 4. Prepare message list for API
                var historyMessages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Add recent chat context
                foreach (var m in Messages.TakeLast(6))
                {
                    historyMessages.Add(new { role = m.Role, content = m.Content });
                }

                var request = new
                {
                    model = new LlmConfigService(_historyRepo.GetRecentMessages(_character.Id, 1).Any() ? "" : "").ChatModelName, // Placeholder for actual config
                    messages = historyMessages,
                    stream = true
                };

                // 5. Stream response
                var assistantMsg = new ChatMessage
                {
                    CharacterId = _character.Id,
                    Role = "assistant",
                    Content = "",
                    Timestamp = DateTime.Now
                };
                Messages.Add(assistantMsg);

                await foreach (var chunk in _chatService.StreamChatAsync(request))
                {
                    assistantMsg.Content += chunk;
                    // Note: In WinUI, we might need DispatcherQueue.TryEnqueue, 
                    // but ObservableCollection handles basic updates if on UI thread.
                }

                _historyRepo.SaveMessage(assistantMsg);
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Role = "system", Content = $"Error: {ex.Message}" });
            }
            finally
            {
                IsTyping = false;
            }
        }
    }
}

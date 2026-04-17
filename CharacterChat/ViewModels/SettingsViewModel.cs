using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CharacterChat.Services;
using CharacterChat.Data;

namespace CharacterChat.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly LlmConfigService _config;

        [ObservableProperty] private string _baseUrl;
        [ObservableProperty] private string _apiKey;
        [ObservableProperty] private string _chatModel;
        [ObservableProperty] private string _embeddingModel;

        public SettingsViewModel()
        {
            // Note: In real app, we'd inject this. For simplicity in this demo, accessing default DB.
            _config = new LlmConfigService(DatabaseService.GetConnectionString("AppGlobal"));
            
            _baseUrl = _config.BaseUrl;
            _apiKey = _config.ApiKey;
            _chatModel = _config.ChatModelName;
            _embeddingModel = _config.EmbeddingsModelName;
        }

        [RelayCommand]
        public void Save()
        {
            _config.SaveSetting("BaseUrl", BaseUrl);
            _config.SaveSetting("ApiKey", ApiKey);
            _config.SaveSetting("ChatModelName", ChatModel);
            _config.SaveSetting("EmbeddingsModelName", EmbeddingModel);
        }
    }
}

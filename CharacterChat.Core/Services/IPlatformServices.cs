using System.Threading.Tasks;

namespace CharacterChat.Services
{
    public interface IFilePickerService
    {
        Task<string?> PickDocumentFileAsync();
    }

    public interface INavigationService
    {
        void NavigateToChat(Models.Character character, string connectionString);
        void NavigateToError(string error);
    }
}

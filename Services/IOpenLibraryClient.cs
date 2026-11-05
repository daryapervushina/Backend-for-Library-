using Library.DTOs.OpenLibrary;
namespace Library.Services
{
    public interface IOpenLibraryClient
    {
        Task<OpenLibraryBookData> GetByIsbnAsync(string isbn);

    }
}

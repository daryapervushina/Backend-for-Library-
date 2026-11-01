using Library.Models;

namespace Library.Services;

public interface IBookLocker
{
    Task<Book> GetForUpdateAsync(int bookId);
}

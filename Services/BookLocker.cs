using Library.Common.Exceptions;
using Library.Data;
using Library.Models;
using Microsoft.EntityFrameworkCore;

namespace Library.Services;

public class BookLocker : IBookLocker
{
    private readonly AppDbContext _db;

    public BookLocker(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Book> GetForUpdateAsync(int bookId)
    {
        var books = await _db.Books
            .FromSqlInterpolated($"SELECT * FROM books WHERE \"Id\" = {bookId} FOR UPDATE")
            .ToListAsync();

        return books.FirstOrDefault() ?? throw new NotFoundException("Book not found");
    }
}

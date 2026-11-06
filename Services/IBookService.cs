using Library.DTOs.Books;
using Library.DTOs.Common;

namespace Library.Services;

public interface IBookService
{
    Task<PagedResponse<BookResponse>> GetBooksAsync(BookQuery query);

    Task<BookResponse> CreateAsync(CreateBookRequest request);

    Task<BookResponse> UpdateAsync(int id, UpdateBookRequest request);
}

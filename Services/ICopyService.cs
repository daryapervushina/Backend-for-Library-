using Library.DTOs.Copies;
using Library.Models.Enums;

namespace Library.Services;

public interface ICopyService
{
    Task<List<CopyResponse>> GetByBookAsync(int bookId, BookCopyStatus? status);

    Task<CopyResponse> CreateAsync(int bookId, CreateCopyRequest request);

    Task<CopyResponse> UpdateAsync(int copyId, UpdateCopyRequest request);
}

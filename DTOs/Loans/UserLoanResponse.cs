using Library.DTOs.Fines;

namespace Library.DTOs.Loans;

public record UserLoanResponse(
    int Id,
    BookShortResponse Book,
    CopyShortResponse Copy,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    string Status,
    bool IsOverdue,
    FineResponse? Fine);

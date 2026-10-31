namespace Library.DTOs.Loans;

public record UserLoanResponse(
    int Id,
    BookShortResponse Book,
    DateTime BorrowedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    string Status,
    bool IsOverdue);

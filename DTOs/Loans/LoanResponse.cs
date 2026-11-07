namespace Library.DTOs.Loans;

public record LoanResponse(
    int Id,
    int BookId,
    int UserId,
    DateTime BorrowedAt,
    DateTime DueAt,
    string Status,
    CopyShortResponse Copy);

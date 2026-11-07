using Library.DTOs.Fines;

namespace Library.DTOs.Loans;

public record ReturnLoanResponse(
    int Id,
    int BookId,
    int UserId,
    string Status,
    DateTime ReturnedAt,
    CopyShortResponse Copy,
    FineResponse? Fine);

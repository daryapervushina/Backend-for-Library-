namespace Library.DTOs.Loans;

public record ReturnLoanResponse(int Id, int BookId, int UserId, string Status, DateTime ReturnedAt);

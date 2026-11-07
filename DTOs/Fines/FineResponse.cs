namespace Library.DTOs.Fines;

public record FineResponse(
    int Id,
    int LoanId,
    int Amount,
    string Reason,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt);

public record UserFinesResponse(List<FineResponse> Items, int TotalAmount);

public record PayFineResponse(int Id, string Status, DateTime? PaidAt);

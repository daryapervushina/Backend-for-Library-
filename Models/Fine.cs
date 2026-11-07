using Library.Models.Enums;

namespace Library.Models;

public class Fine
{
    public int Id { get; set; }
    public int LoanId { get; set; }
    public int UserId { get; set; }

    /// <summary>Amount in rubles.</summary>
    public int Amount { get; set; }

    public FineReason Reason { get; set; }
    public FineStatus Status { get; set; } = FineStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public User User { get; set; } = null!;
    public Loan Loan { get; set; } = null!;
}

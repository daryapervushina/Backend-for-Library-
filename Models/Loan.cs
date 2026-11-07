using Library.Models.Enums;

namespace Library.Models;

public class Loan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public int CopyId { get; set; }
    public DateTime BorrowedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public BookCopy Copy { get; set; } = null!;
    public Fine? Fine { get; set; }
}

using Library.Models.Enums;

namespace Library.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }

    /// <summary>Concrete copy held for the user. Set when the reservation becomes READY.</summary>
    public int? CopyId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public BookCopy? Copy { get; set; }
}

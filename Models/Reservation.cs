using Library.Models.Enums;

namespace Library.Models;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Заполняется только когда бронь переходит в READY (now + 48h).</summary>
    public DateTime? ExpiresAt { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Waiting;

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
}

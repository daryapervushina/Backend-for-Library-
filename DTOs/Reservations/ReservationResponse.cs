namespace Library.DTOs.Reservations;

public record ReservationResponse(int Id, int BookId, int UserId, DateTime CreatedAt, DateTime? ExpiresAt, string Status);

using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Reservations;

public class CreateReservationRequest
{
    [Required]
    public int? BookId { get; set; }
}

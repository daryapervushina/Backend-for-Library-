using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Loans;

public class CreateLoanRequest
{
    [Required]
    public int? BookId { get; set; }
}

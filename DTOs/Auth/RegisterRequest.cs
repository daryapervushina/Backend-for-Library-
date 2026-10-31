using System.ComponentModel.DataAnnotations;
using Library.Common;

namespace Library.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "is required")]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = "invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "is required")]
    [MinLength(8, ErrorMessage = "must be at least 8 characters")]
    [RegularExpression(ValidationPatterns.PasswordLetterAndDigit, ErrorMessage = "must contain at least one letter and one digit")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "is required")]
    public string Name { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "is required")]
    public string Password { get; set; } = string.Empty;
}

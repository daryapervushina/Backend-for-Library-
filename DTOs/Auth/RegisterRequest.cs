using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;
}

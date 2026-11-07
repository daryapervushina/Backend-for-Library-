using Library.Models.Enums;

namespace Library.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Reader;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}

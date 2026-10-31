namespace Library.DTOs.Auth;

public record UserResponse(int Id, string Email, string Name, string Role, bool IsActive, DateTime CreatedAt);

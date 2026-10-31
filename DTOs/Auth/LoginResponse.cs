namespace Library.DTOs.Auth;

public record LoginResponse(string Token, UserShortResponse User);

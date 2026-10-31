using Library.DTOs.Auth;

namespace Library.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request);

    Task<LoginResponse> LoginAsync(LoginRequest request);
}

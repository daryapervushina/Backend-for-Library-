using Library.DTOs.Auth;
using Library.Filters;
using Library.Services;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [ServiceFilter(typeof(ValidationFilter), Order = -3000)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        return Ok(await _authService.RegisterAsync(request));
    }

    [HttpPost("login")]
    [ServiceFilter(typeof(ValidationFilter), Order = -3000)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        return Ok(await _authService.LoginAsync(request));
    }
}

using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Auth;
using Library.Models;
using Library.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthService(AppDbContext db, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new ConflictException("Email is already registered");
        }

        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            Role = UserRole.Reader,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("Email is already registered");
        }

        return new UserResponse(user.Id, user.Email, user.Name, user.Role.ToApiString(), user.IsActive, user.CreatedAt);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        var check = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        var token = _tokenGenerator.Generate(user);

        return new LoginResponse(token, new UserShortResponse(user.Id, user.Email, user.Name, user.Role.ToApiString()));
    }
}

using Library.Common;
using Library.Models;
using Library.Models.Enums;
using Library.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Library.Data;

public class DbSeeder : IDbSeeder
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly SeedOptions _options;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public DbSeeder(AppDbContext db, IClock clock, IOptions<SeedOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public async Task SeedAsync()
    {
        if (await _db.Users.AnyAsync(u => u.Role == UserRole.Librarian))
        {
            return;
        }

        var librarian = new User
        {
            Email = _options.LibrarianEmail,
            Name = _options.LibrarianName,
            Role = UserRole.Librarian,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };
        librarian.PasswordHash = _passwordHasher.HashPassword(librarian, _options.LibrarianPassword);

        _db.Users.Add(librarian);
        await _db.SaveChangesAsync();
    }
}

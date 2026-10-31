using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Library.Common;
using Library.Models;
using Library.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Library.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public string Generate(User user)
    {
        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("role", user.Role.ToApiString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = _clock.UtcNow;

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

using Library.Models;

namespace Library.Services;

public interface IJwtTokenGenerator
{
    string Generate(User user);
}

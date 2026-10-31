namespace Library.Common;

public static class ValidationPatterns
{
    public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string PasswordLetterAndDigit = @"^(?=.*[A-Za-z])(?=.*\d).+$";
}

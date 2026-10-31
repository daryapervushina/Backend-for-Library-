namespace Library.Options;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string LibrarianEmail { get; set; } = string.Empty;
    public string LibrarianPassword { get; set; } = string.Empty;
    public string LibrarianName { get; set; } = string.Empty;
}

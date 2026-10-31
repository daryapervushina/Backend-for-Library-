namespace Library.Options;

public class OpenLibraryOptions
{
    public const string SectionName = "OpenLibrary";

    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; }
}

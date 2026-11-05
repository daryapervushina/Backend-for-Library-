 namespace Library.DTOs.OpenLibrary;

public record OpenLibraryBookData(
    string Title,
    string? Description,
    int? PublicationYear,
    List<string> AuthorNames);

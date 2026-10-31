namespace Library.DTOs.Books;

public record BookResponse(
    int Id,
    string Title,
    string? Isbn,
    string? Description,
    int? PublicationYear,
    int TotalCopies,
    int AvailableCopies,
    List<AuthorShortResponse> Authors,
    List<CategoryShortResponse> Categories);

namespace Library.DTOs.Books;

public record CopiesSummaryResponse(
    int Available,
    int Loaned,
    int Reserved,
    int Maintenance,
    int Lost,
    int Damaged);

public record BookResponse(
    int Id,
    string Title,
    string? Isbn,
    string? Description,
    int? PublicationYear,
    int TotalCopies,
    int AvailableCopies,
    List<AuthorShortResponse> Authors,
    List<CategoryShortResponse> Categories,
    CopiesSummaryResponse Copies);

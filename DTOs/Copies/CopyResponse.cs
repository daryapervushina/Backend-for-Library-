namespace Library.DTOs.Copies;

public record CopyResponse(
    int Id,
    int BookId,
    int InventoryNumber,
    string Status,
    string Condition,
    DateTime CreatedAt);

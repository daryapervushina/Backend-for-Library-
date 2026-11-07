using Library.Models.Enums;

namespace Library.Models;

public class BookCopy
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int InventoryNumber { get; set; }
    public BookCopyStatus Status { get; set; } = BookCopyStatus.Available;
    public BookCopyCondition Condition { get; set; } = BookCopyCondition.Ok;
    public DateTime CreatedAt { get; set; }

    public Book Book { get; set; } = null!;
}

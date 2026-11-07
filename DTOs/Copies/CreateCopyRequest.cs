using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Copies;

public class CreateCopyRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int? InventoryNumber { get; set; }

    /// <summary>OK (default), DAMAGED or LOST.</summary>
    public string? Condition { get; set; }
}

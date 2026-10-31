using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Books;

public class UpdateBookRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? PublicationYear { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? TotalCopies { get; set; }

    [Required]
    public List<int> AuthorIds { get; set; } = new();

    [Required]
    public List<int> CategoryIds { get; set; } = new();
}

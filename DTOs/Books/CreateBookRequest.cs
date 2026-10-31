using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Books;

public class CreateBookRequest
{
    public string? Isbn { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? PublicationYear { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int? TotalCopies { get; set; }

    public List<int>? AuthorIds { get; set; }

    public List<int>? CategoryIds { get; set; }
}

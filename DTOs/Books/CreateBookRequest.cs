using System.ComponentModel.DataAnnotations;

namespace Library.DTOs.Books;

public class CreateBookRequest : IValidatableObject
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Isbn) && string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("is required when isbn is not provided", new[] { nameof(Title) });
        }
    }
}

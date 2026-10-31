using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Library.DTOs.Books;

public class BookQuery
{
    public string? Search { get; set; }

    public string? Isbn { get; set; }

    [FromQuery(Name = "category_id")]
    public int? CategoryId { get; set; }

    [FromQuery(Name = "author_id")]
    public int? AuthorId { get; set; }

    public bool? Available { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int Limit { get; set; } = 20;
}

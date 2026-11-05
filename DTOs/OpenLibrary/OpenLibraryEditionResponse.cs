using System.Text.Json.Serialization;

namespace Library.DTOs.OpenLibrary;

public class OpenLibraryEditionResponse
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("authors")]
    public List<OpenLibraryKeyReference>? Authors { get; set; }

    [JsonPropertyName("number_of_pages")]
    public int? NumberOfPages { get; set; }

    [JsonPropertyName("publish_date")]
    public string? PublishDate { get; set; }
}

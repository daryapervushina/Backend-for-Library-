 using System.Text.Json.Serialization;

namespace Library.DTOs.OpenLibrary;

public class OpenLibraryAuthorResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

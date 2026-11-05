using System.Text.Json.Serialization;

namespace Library.DTOs.OpenLibrary;

public class OpenLibraryKeyReference
{
    [JsonPropertyName("key")]
    public string? Key { get; set; }
}

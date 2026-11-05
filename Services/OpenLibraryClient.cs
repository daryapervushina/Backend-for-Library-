using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Library.Common.Exceptions;
using Library.DTOs.OpenLibrary;
using Library.Options;

namespace Library.Services;

public class OpenLibraryClient : IOpenLibraryClient
{
    private static readonly Regex YearPattern = new(@"\b(\d{4})\b");

    private readonly IHttpClientFactory _httpClientFactory;

    public OpenLibraryClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OpenLibraryBookData> GetByIsbnAsync(string isbn)
    {
        var client = _httpClientFactory.CreateClient(OpenLibraryOptions.ClientName);

        var edition = await GetJsonAsync<OpenLibraryEditionResponse>(client, $"isbn/{Uri.EscapeDataString(isbn)}.json")
            ?? throw new NotFoundException($"book with isbn {isbn} not found in Open Library");

        if (string.IsNullOrWhiteSpace(edition.Title))
            throw new ServiceUnavailableException("Open Library returned incomplete data");

        var authorNames = await GetAuthorNamesAsync(client, edition.Authors);

        return new OpenLibraryBookData(
            edition.Title,
            BuildDescription(edition.NumberOfPages),
            ParseYear(edition.PublishDate),
            authorNames);
    }

    private static async Task<List<string>> GetAuthorNamesAsync(HttpClient client, List<OpenLibraryKeyReference>? authors)
    {
        var names = new List<string>();

        if (authors == null)
            return names;

        foreach (var author in authors)
        {
            if (string.IsNullOrWhiteSpace(author.Key))
                continue;

            var response = await GetJsonAsync<OpenLibraryAuthorResponse>(client, $"{author.Key.TrimStart('/')}.json");

            if (!string.IsNullOrWhiteSpace(response?.Name) && !names.Contains(response.Name))
                names.Add(response.Name);
        }

        return names;
    }

    private static async Task<T?> GetJsonAsync<T>(HttpClient client, string path) where T : class
    {
        try
        {
            using var response = await client.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new ServiceUnavailableException("Open Library is unavailable");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (HttpRequestException)
        {
            throw new ServiceUnavailableException("Open Library is unavailable");
        }
        catch (TaskCanceledException)
        {
            throw new ServiceUnavailableException("Open Library did not respond in time");
        }
        catch (JsonException)
        {
            throw new ServiceUnavailableException("Open Library returned invalid data");
        }
    }

    private static string? BuildDescription(int? numberOfPages)
    {
        return numberOfPages == null ? null : $"Number of pages: {numberOfPages}";
    }

    private static int? ParseYear(string? publishDate)
    {
        if (string.IsNullOrWhiteSpace(publishDate))
            return null;

        var match = YearPattern.Match(publishDate);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }
}

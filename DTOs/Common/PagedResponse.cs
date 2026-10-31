namespace Library.DTOs.Common;

public record PagedResponse<T>(List<T> Items, int Total, int Page, int Limit);

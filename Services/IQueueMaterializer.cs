using Library.Models;

namespace Library.Services;

public interface IQueueMaterializer
{
    Task MaterializeAsync(Book book, DateTime now);

    Task MaterializeAllAsync(DateTime now);
}

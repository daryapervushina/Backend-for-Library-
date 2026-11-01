using Library.Models;

namespace Library.Services;

public interface IReservationQueue
{
    Task ReleaseCopyAsync(Book book, DateTime now);
}

using Library.Data;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services;

public class QueueMaterializer : IQueueMaterializer
{
    private readonly AppDbContext _db;
    private readonly IBookLocker _bookLocker;
    private readonly IReservationQueue _reservationQueue;

    public QueueMaterializer(AppDbContext db, IBookLocker bookLocker, IReservationQueue reservationQueue)
    {
        _db = db;
        _bookLocker = bookLocker;
        _reservationQueue = reservationQueue;
    }

    public async Task MaterializeAsync(Book book, DateTime now)
    {
        while (true)
        {
            var expired = await _db.Reservations
                .Where(r => r.BookId == book.Id
                    && r.Status == ReservationStatus.Ready
                    && r.ExpiresAt < now)
                .OrderBy(r => r.ExpiresAt)
                .FirstOrDefaultAsync();

            if (expired is null)
            {
                return; 
            }

            expired.Status = ReservationStatus.Expired;
            await _reservationQueue.ReleaseCopyAsync(book, now);
        }
    }

    public async Task MaterializeAllAsync(DateTime now)
    {
        var bookIds = await _db.Reservations
            .Where(r => r.Status == ReservationStatus.Ready && r.ExpiresAt < now)
            .Select(r => r.BookId)
            .Distinct()
            .ToListAsync();

        foreach (var bookId in bookIds)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var book = await _bookLocker.GetForUpdateAsync(bookId);
            await MaterializeAsync(book, now);

            await transaction.CommitAsync();
        }
    }
}

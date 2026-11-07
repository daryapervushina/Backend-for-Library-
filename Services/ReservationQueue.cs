using Library.Common;
using Library.Data;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services;

public class ReservationQueue : IReservationQueue
{
    private readonly AppDbContext _db;

    public ReservationQueue(AppDbContext db)
    {
        _db = db;
    }

    public async Task ReleaseCopyAsync(Book book, BookCopy copy, DateTime now)
    {
        var next = await _db.Reservations
            .Where(r => r.BookId == book.Id && r.Status == ReservationStatus.Waiting)
            .OrderBy(r => r.CreatedAt)
            .ThenBy(r => r.Id)
            .FirstOrDefaultAsync();

        if (next is null)
        {
            copy.Status = BookCopyStatus.Available;
        }
        else
        {
            copy.Status = BookCopyStatus.Reserved;
            next.Status = ReservationStatus.Ready;
            next.CopyId = copy.Id;
            next.ExpiresAt = now.AddHours(LibraryRules.ReservationHoldHours);
        }

        await _db.SaveChangesAsync();
    }
}

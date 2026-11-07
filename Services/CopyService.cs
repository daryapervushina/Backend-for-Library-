using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Copies;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class CopyService : ICopyService
{
    private readonly AppDbContext _db;
    private readonly IBookLocker _bookLocker;
    private readonly IQueueMaterializer _materializer;
    private readonly IReservationQueue _reservationQueue;

    public CopyService(
        AppDbContext db,
        IBookLocker bookLocker,
        IQueueMaterializer materializer,
        IReservationQueue reservationQueue)
    {
        _db = db;
        _bookLocker = bookLocker;
        _materializer = materializer;
        _reservationQueue = reservationQueue;
    }

    public async Task<List<CopyResponse>> GetByBookAsync(int bookId, BookCopyStatus? status)
    {
        if (!await _db.Books.AnyAsync(b => b.Id == bookId))
            throw new NotFoundException("book not found");

        
        var query = _db.BookCopies.AsNoTracking().Where(c => c.BookId == bookId);

        if (status != null)
            query = query.Where(c => c.Status == status.Value);

        var copies = await query.OrderBy(c => c.Id).ToListAsync();

        return copies.Select(ToResponse).ToList();
    }

    public async Task<CopyResponse> CreateAsync(int bookId, CreateCopyRequest request)
    {
        var now = DateTime.UtcNow;
        var inventoryNumber = request.InventoryNumber!.Value;

        var condition = BookCopyCondition.Ok;
        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            condition = EnumParsing.Parse<BookCopyCondition>(request.Condition, "condition");
            EnsureNotSetManually(condition);
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var book = await _bookLocker.GetForUpdateAsync(bookId);
        await _materializer.MaterializeAsync(book, now);

        if (await _db.BookCopies.AnyAsync(c => c.InventoryNumber == inventoryNumber))
            throw new ConflictException("inventory_number is already used");

        var copy = new BookCopy
        {
            BookId = book.Id,
            InventoryNumber = inventoryNumber,
            Condition = condition,
            Status = condition switch
            {
                BookCopyCondition.Damaged => BookCopyStatus.Maintenance,
                BookCopyCondition.Lost => BookCopyStatus.Lost,
                _ => BookCopyStatus.Available
            },
            CreatedAt = now
        };

        _db.BookCopies.Add(copy);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("inventory_number is already used");
        }

        if (copy.Status == BookCopyStatus.Available)
        {
             await _reservationQueue.ReleaseCopyAsync(book, copy, now);
        }

        await tx.CommitAsync();

        return ToResponse(copy);
    }

    public async Task<CopyResponse> UpdateAsync(int copyId, UpdateCopyRequest request)
    {
        var now = DateTime.UtcNow;

        BookCopyStatus? newStatus = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            newStatus = EnumParsing.Parse<BookCopyStatus>(request.Status, "status");
            EnsureNotSetManually(newStatus.Value);
        }

        BookCopyCondition? newCondition = null;
        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            newCondition = EnumParsing.Parse<BookCopyCondition>(request.Condition, "condition");
            EnsureNotSetManually(newCondition.Value);
        }

        if (newStatus == null && newCondition == null)
            throw new BadRequestException("status or condition is required");

        var info = await _db.BookCopies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == copyId);
        if (info == null)
            throw new NotFoundException("copy not found");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var book = await _bookLocker.GetForUpdateAsync(info.BookId);
        await _materializer.MaterializeAsync(book, now);

        var copy = await _db.BookCopies.FirstAsync(c => c.Id == copyId);

        if (newStatus != null && copy.Status is BookCopyStatus.Loaned or BookCopyStatus.Reserved)
        {
            throw new ConflictException(
                "copy is loaned or reserved: return the loan or cancel the reservation first");
        }

        if (newCondition != null)
            copy.Condition = newCondition.Value;

        if (newStatus == BookCopyStatus.Available && copy.Status != BookCopyStatus.Available)
        {
            await _reservationQueue.ReleaseCopyAsync(book, copy, now);
        }
        else
        {
            if (newStatus != null)
                copy.Status = newStatus.Value;

            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        return ToResponse(copy);
    }
    private static void EnsureNotSetManually(BookCopyStatus status)
    {
        if (status is BookCopyStatus.Loaned or BookCopyStatus.Reserved)
            throw new ConflictException("LOANED and RESERVED can only be set through loans and reservations");
    }

    private static void EnsureNotSetManually(BookCopyCondition condition)
    {
        if (condition is BookCopyCondition.Loaned or BookCopyCondition.Reserved)
            throw new ConflictException("LOANED and RESERVED can only be set through loans and reservations");
    }

    private static CopyResponse ToResponse(BookCopy c) => new(
        c.Id, c.BookId, c.InventoryNumber, c.Status.ToApiString(), c.Condition.ToApiString(), c.CreatedAt);
}

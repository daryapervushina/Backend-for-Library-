using System.Linq.Expressions;
using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Books;
using Library.DTOs.Common;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Library.Services;

public class BookService : IBookService
{
    private static readonly Expression<Func<Book, BookResponse>> ToResponse = b => new BookResponse(
        b.Id,
        b.Title,
        b.Isbn,
        b.Description,
        b.PublicationYear,
        b.TotalCopies,
        b.AvailableCopies,
        b.Authors.OrderBy(a => a.Id).Select(a => new AuthorShortResponse(a.Id, a.Name)).ToList(),
        b.Categories.OrderBy(c => c.Id).Select(c => new CategoryShortResponse(c.Id, c.Name)).ToList());

    private readonly AppDbContext _db;
    private readonly IOpenLibraryClient _openLibrary;
    private readonly IBookLocker _bookLocker;
    private readonly IQueueMaterializer _materializer;
    private readonly IReservationQueue _reservationQueue;

    public BookService(
        AppDbContext db,
        IOpenLibraryClient openLibrary,
        IBookLocker bookLocker,
        IQueueMaterializer materializer,
        IReservationQueue reservationQueue)
    {
        _db = db;
        _openLibrary = openLibrary;
        _bookLocker = bookLocker;
        _materializer = materializer;
        _reservationQueue = reservationQueue;
    }

    public async Task<PagedResponse<BookResponse>> GetBooksAsync(BookQuery query)
    {
        await _materializer.MaterializeAllAsync(DateTime.UtcNow);

        var books = _db.Books.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            books = books.Where(b =>
                EF.Functions.ILike(b.Title, pattern) ||
                (b.Description != null && EF.Functions.ILike(b.Description, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(query.Isbn))
        {
            var isbn = query.Isbn.Trim();
            books = books.Where(b => b.Isbn == isbn);
        }

        if (query.CategoryId != null)
            books = books.Where(b => b.Categories.Any(c => c.Id == query.CategoryId));

        if (query.AuthorId != null)
            books = books.Where(b => b.Authors.Any(a => a.Id == query.AuthorId));

        if (query.Available == true)
            books = books.Where(b => b.AvailableCopies > 0);
        else if (query.Available == false)
            books = books.Where(b => b.AvailableCopies == 0);

        var total = await books.CountAsync();

        var items = await books
            .OrderBy(b => b.Id)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .Select(ToResponse)
            .ToListAsync();

        return new PagedResponse<BookResponse>(items, total, query.Page, query.Limit);
    }

    public async Task<BookResponse> CreateAsync(CreateBookRequest request)
    {
        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
        var totalCopies = request.TotalCopies!.Value;

        if (isbn != null && await _db.Books.AnyAsync(b => b.Isbn == isbn))
            throw new ConflictException("book with this isbn already exists");

        var book = new Book
        {
            Isbn = isbn,
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies,
            CreatedAt = DateTime.UtcNow
        };

        if (isbn != null)
        {
            var data = await _openLibrary.GetByIsbnAsync(isbn);

            book.Title = data.Title;
            book.Description = data.Description ?? request.Description;
            book.PublicationYear = data.PublicationYear ?? request.PublicationYear;

            foreach (var name in data.AuthorNames)
                book.Authors.Add(await FindOrCreateAuthorAsync(name));
        }
        else
        {
            book.Title = request.Title!.Trim();
            book.Description = request.Description;
            book.PublicationYear = request.PublicationYear;

            foreach (var author in await LoadAuthorsAsync(request.AuthorIds))
                book.Authors.Add(author);
        }

        foreach (var category in await LoadCategoriesAsync(request.CategoryIds))
            book.Categories.Add(category);

        _db.Books.Add(book);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("book with this isbn already exists");
        }

        return await GetResponseAsync(book.Id);
    }

    public async Task<BookResponse> UpdateAsync(int id, UpdateBookRequest request)
    {
        var now = DateTime.UtcNow;
        var newTotal = request.TotalCopies!.Value;

        await using var tx = await _db.Database.BeginTransactionAsync();

        var book = await _bookLocker.GetForUpdateAsync(id);
        await _materializer.MaterializeAsync(book, now);

        var activeLoans = await _db.Loans.CountAsync(l => l.BookId == id && l.Status == LoanStatus.Active);
        var readyReservations = await _db.Reservations.CountAsync(r => r.BookId == id && r.Status == ReservationStatus.Ready);

        if (newTotal < activeLoans + readyReservations)
            throw new ConflictException("total_copies cannot be less than active loans plus ready reservations");

        var authors = await LoadAuthorsAsync(request.AuthorIds);
        var categories = await LoadCategoriesAsync(request.CategoryIds);

        await _db.Entry(book).Collection(b => b.Authors).LoadAsync();
        await _db.Entry(book).Collection(b => b.Categories).LoadAsync();

        book.Title = request.Title.Trim();
        book.Description = request.Description;
        book.PublicationYear = request.PublicationYear;

        book.Authors.Clear();
        foreach (var author in authors)
            book.Authors.Add(author);

        book.Categories.Clear();
        foreach (var category in categories)
            book.Categories.Add(category);

        var freeCopies = newTotal - activeLoans - readyReservations;
        var waiting = await _db.Reservations.CountAsync(r => r.BookId == id && r.Status == ReservationStatus.Waiting);
        var toQueue = Math.Min(freeCopies, waiting);

        book.TotalCopies = newTotal;
        book.AvailableCopies = freeCopies - toQueue;

        for (var i = 0; i < toQueue; i++)
            await _reservationQueue.ReleaseCopyAsync(book, now);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return await GetResponseAsync(id);
    }

    private async Task<BookResponse> GetResponseAsync(int id)
    {
        return await _db.Books
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(ToResponse)
            .FirstAsync();
    }

    private async Task<Author> FindOrCreateAuthorAsync(string name)
    {
        return await _db.Authors.FirstOrDefaultAsync(a => a.Name == name)
            ?? new Author { Name = name };
    }

    private async Task<List<Author>> LoadAuthorsAsync(List<int>? ids)
    {
        if (ids == null || ids.Count == 0)
            return new List<Author>();

        var distinctIds = ids.Distinct().ToList();
        var authors = await _db.Authors.Where(a => distinctIds.Contains(a.Id)).ToListAsync();

        if (authors.Count != distinctIds.Count)
            throw new BadRequestException("one or more authors not found");

        return authors;
    }

    private async Task<List<Category>> LoadCategoriesAsync(List<int>? ids)
    {
        if (ids == null || ids.Count == 0)
            return new List<Category>();

        var distinctIds = ids.Distinct().ToList();
        var categories = await _db.Categories.Where(c => distinctIds.Contains(c.Id)).ToListAsync();

        if (categories.Count != distinctIds.Count)
            throw new BadRequestException("one or more categories not found");

        return categories;
    }
}

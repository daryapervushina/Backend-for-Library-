using System.Linq.Expressions;
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
    private const long InventoryLockKey = 7001;

     private static readonly Expression<Func<Book, BookResponse>> ToResponse = b => new BookResponse(
        b.Id,
        b.Title,
        b.Isbn,
        b.Description,
        b.PublicationYear,
        b.Copies.Count(),
        b.Copies.Count(c => c.Status == BookCopyStatus.Available),
        b.Authors.OrderBy(a => a.Id).Select(a => new AuthorShortResponse(a.Id, a.Name)).ToList(),
        b.Categories.OrderBy(c => c.Id).Select(c => new CategoryShortResponse(c.Id, c.Name)).ToList(),
        new CopiesSummaryResponse(
            b.Copies.Count(c => c.Status == BookCopyStatus.Available),
            b.Copies.Count(c => c.Status == BookCopyStatus.Loaned),
            b.Copies.Count(c => c.Status == BookCopyStatus.Reserved),
            b.Copies.Count(c => c.Status == BookCopyStatus.Maintenance),
            b.Copies.Count(c => c.Status == BookCopyStatus.Lost),
            b.Copies.Count(c => c.Status == BookCopyStatus.Damaged)));

    private readonly AppDbContext _db;
    private readonly IOpenLibraryClient _openLibrary;
    private readonly IQueueMaterializer _materializer;

    public BookService(
        AppDbContext db,
        IOpenLibraryClient openLibrary,
        IQueueMaterializer materializer)
    {
        _db = db;
        _openLibrary = openLibrary;
        _materializer = materializer;
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
            books = books.Where(b => b.Copies.Any(c => c.Status == BookCopyStatus.Available));
        else if (query.Available == false)
            books = books.Where(b => !b.Copies.Any(c => c.Status == BookCopyStatus.Available));

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
        var initialCopies = request.TotalCopies!.Value;

        if (isbn != null && await _db.Books.AnyAsync(b => b.Isbn == isbn))
            throw new ConflictException("book with this isbn already exists");

        var book = new Book
        {
            Isbn = isbn,
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

        await using var tx = await _db.Database.BeginTransactionAsync();

         if (initialCopies > 0)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({InventoryLockKey})");

            var next = (await _db.BookCopies.MaxAsync(c => (int?)c.InventoryNumber) ?? 0) + 1;

            for (var i = 0; i < initialCopies; i++)
            {
                book.Copies.Add(new BookCopy
                {
                    InventoryNumber = next + i,
                    Status = BookCopyStatus.Available,
                    Condition = BookCopyCondition.Ok,
                    CreatedAt = book.CreatedAt
                });
            }
        }

        _db.Books.Add(book);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            throw pg.ConstraintName?.Contains("InventoryNumber") == true
                ? new ConflictException("inventory_number is already used, try again")
                : new ConflictException("book with this isbn already exists");
        }

        await tx.CommitAsync();

        return await GetResponseAsync(book.Id);
    }

    public async Task<BookResponse> UpdateAsync(int id, UpdateBookRequest request)
    {
        if (request.TotalCopies != null || request.AvailableCopies != null)
        {
            throw new BadRequestException(
                "total_copies and available_copies cannot be changed directly; manage copies via /api/books/{id}/copies");
        }

        var authors = await LoadAuthorsAsync(request.AuthorIds);
        var categories = await LoadCategoriesAsync(request.CategoryIds);

        var book = await _db.Books
            .Include(b => b.Authors)
            .Include(b => b.Categories)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new NotFoundException("Book not found");

        book.Title = request.Title.Trim();
        book.Description = request.Description;
        book.PublicationYear = request.PublicationYear;

        book.Authors.Clear();
        foreach (var author in authors)
            book.Authors.Add(author);

        book.Categories.Clear();
        foreach (var category in categories)
            book.Categories.Add(category);

        await _db.SaveChangesAsync();

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

using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Loans;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Library.Services
{
    public class LoanService:ILoanService
    {
        private readonly AppDbContext _db;
        private readonly ILoanPolicyChecker _policyChecker;
        private readonly IBookLocker _bookLocker;
        private readonly IQueueMaterializer _materializer;
        private readonly IReservationQueue _reservation;
        public LoanService(AppDbContext db, IReservationQueue reservation,
            ILoanPolicyChecker policyChecker,IBookLocker bookLocker, IQueueMaterializer materializer)
        {
            _db = db;
            _policyChecker = policyChecker;
            _bookLocker = bookLocker;
            _materializer = materializer;
            _reservation = reservation;

        }
        public async Task<LoanResponse> BorrowAsync(int userId, CreateLoanRequest request)
        {
            var now = DateTime.UtcNow;
            var bookId = request.BookId!.Value;

            await using var tx = await _db.Database.BeginTransactionAsync();

            await _policyChecker.EnsureCanBorrowAsync(userId, now);
            var book = await _bookLocker.GetForUpdateAsync(bookId);
            await _materializer.MaterializeAsync(book, now);

            var reservation = await _db.Reservations.FirstOrDefaultAsync(r =>
                r.BookId == bookId && r.UserId == userId && r.Status == ReservationStatus.Ready);

            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Fulfilled;
            }
            else if (book.AvailableCopies > 0)
            {
                book.AvailableCopies--;
            }
            else
            {
                throw new ConflictException("No available copies");
            }

            var loan = new Loan
            {
                UserId = userId,
                BookId = bookId,
                BorrowedAt = now,
                DueAt = now.AddDays(LibraryRules.LoanDurationDays),
                Status = LoanStatus.Active
            };

            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new LoanResponse(loan.Id, loan.BookId, loan.UserId, loan.BorrowedAt, loan.DueAt, "ACTIVE");
        }

        public async Task<ReturnLoanResponse> ReturnAsync(int id, int userId, bool isLibrarian)
        {
            var now = DateTime.UtcNow;
            var loanInfo = await _db.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
            if (loanInfo == null)
            {
                throw new NotFoundException("loan not found");
            }
            if (!isLibrarian && userId != loanInfo.UserId)
            {
                throw new ForbiddenException("you don't have permission to access this resource");
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            var book = await _bookLocker.GetForUpdateAsync(loanInfo.BookId);
            var loan = await _db.Loans.FirstAsync(l => l.Id == id);

            if (loan.Status == LoanStatus.Returned)
            {
                throw new ConflictException("loan has already been returned");
            }

            await _materializer.MaterializeAsync(book, now);

            loan.Status = LoanStatus.Returned;
            loan.ReturnedAt = now;

            await _reservation.ReleaseCopyAsync(book, now);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new ReturnLoanResponse(loan.Id, loan.BookId, loan.UserId, "RETURNED", now);
        }
        public async Task <List<UserLoanResponse>> GetUserLoansAsync(int userId, int requesterId, bool isLibrarian,LoanFilter? filter)
        {
            var now = DateTime.UtcNow;
            if (!isLibrarian && userId != requesterId)
            {
                throw new ForbiddenException("you don't have permission to access this resource");
            }
            if (!await _db.Users.AnyAsync(u => u.Id == userId))
                throw new NotFoundException("user not found");
            var query = _db.Loans.Where(l => l.UserId == userId);
            if (filter == LoanFilter.Active)
                query = query.Where(l => l.Status == LoanStatus.Active);
            else if (filter == LoanFilter.Overdue)
                query = query.Where(l => l.Status == LoanStatus.Active && l.DueAt < now);
            else if (filter == LoanFilter.History)
                query = query.Where(l => l.Status == LoanStatus.Returned);

            return await query
                .OrderByDescending(l => l.BorrowedAt)
                .Select(l => new UserLoanResponse(
                    l.Id,
                    new BookShortResponse(l.Book.Id, l.Book.Title),
                    l.BorrowedAt,
                    l.DueAt,
                    l.ReturnedAt,
                    l.Status == LoanStatus.Active ? "ACTIVE" : "RETURNED",
                    l.Status == LoanStatus.Active && l.DueAt < now))
                .ToListAsync();
        }
    }
}
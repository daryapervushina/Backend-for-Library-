using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Fines;
using Library.DTOs.Loans;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services
{
    public class LoanService : ILoanService
    {
        private readonly AppDbContext _db;
        private readonly ILoanPolicyChecker _policyChecker;
        private readonly IBookLocker _bookLocker;
        private readonly IQueueMaterializer _materializer;
        private readonly IReservationQueue _reservation;

        public LoanService(AppDbContext db, IReservationQueue reservation,
            ILoanPolicyChecker policyChecker, IBookLocker bookLocker, IQueueMaterializer materializer)
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

            var reservation = await _db.Reservations
                .Include(r => r.Copy)
                .FirstOrDefaultAsync(r =>
                    r.BookId == bookId && r.UserId == userId && r.Status == ReservationStatus.Ready);

            BookCopy copy;

            if (reservation?.Copy != null)
            {
                 reservation.Status = ReservationStatus.Fulfilled;
                copy = reservation.Copy;
            }
            else
            {
                copy = await _db.BookCopies
                    .Where(c => c.BookId == bookId && c.Status == BookCopyStatus.Available)
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync()
                    ?? throw new ConflictException("No available copies");
            }

            copy.Status = BookCopyStatus.Loaned;

            var loan = new Loan
            {
                UserId = userId,
                BookId = bookId,
                Copy = copy,
                BorrowedAt = now,
                DueAt = now.AddDays(LibraryRules.LoanDurationDays),
                Status = LoanStatus.Active
            };

            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new LoanResponse(
                loan.Id, loan.BookId, loan.UserId, loan.BorrowedAt, loan.DueAt,
                loan.Status.ToApiString(), new CopyShortResponse(copy.Id, copy.InventoryNumber));
        }

        public async Task<ReturnLoanResponse> ReturnAsync(int id, int userId, bool isLibrarian, string? conditionValue)
        {
            var now = DateTime.UtcNow;

            var condition = BookCopyCondition.Ok;
            if (!string.IsNullOrWhiteSpace(conditionValue))
            {
                condition = EnumParsing.Parse<BookCopyCondition>(conditionValue, "condition");
                if (condition is not (BookCopyCondition.Ok or BookCopyCondition.Damaged or BookCopyCondition.Lost))
                {
                    throw new BadRequestException("condition must be one of OK, DAMAGED, LOST");
                }
            }

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
            var loan = await _db.Loans.Include(l => l.Copy).FirstAsync(l => l.Id == id);

            if (loan.Status == LoanStatus.Returned)
            {
                throw new ConflictException("loan has already been returned");
            }

            await _materializer.MaterializeAsync(book, now);

            loan.Status = LoanStatus.Returned;
            loan.ReturnedAt = now;

            var copy = loan.Copy;
            var fine = BuildFine(loan, condition, now);
            if (fine != null)
            {
                _db.Fines.Add(fine);
            }

            switch (condition)
            {
                case BookCopyCondition.Damaged:
                    copy.Status = BookCopyStatus.Maintenance;
                    copy.Condition = BookCopyCondition.Damaged;
                    await _db.SaveChangesAsync();
                    break;

                case BookCopyCondition.Lost:
                    copy.Status = BookCopyStatus.Lost;
                    copy.Condition = BookCopyCondition.Lost;
                    await _db.SaveChangesAsync();
                    break;

                default:
                    copy.Condition = BookCopyCondition.Ok;
                    await _reservation.ReleaseCopyAsync(book, copy, now);
                    break;
            }

            await tx.CommitAsync();

            return new ReturnLoanResponse(
                loan.Id, loan.BookId, loan.UserId, loan.Status.ToApiString(), now,
                new CopyShortResponse(copy.Id, copy.InventoryNumber),
                fine == null ? null : ToFineResponse(fine));
        }

        public async Task<List<UserLoanResponse>> GetUserLoansAsync(int userId, int requesterId, bool isLibrarian, LoanFilter? filter)
        {
            var now = DateTime.UtcNow;

            if (!isLibrarian && userId != requesterId)
            {
                throw new ForbiddenException("you don't have permission to access this resource");
            }

            if (!await _db.Users.AnyAsync(u => u.Id == userId))
                throw new NotFoundException("user not found");

            var query = _db.Loans.AsNoTracking().Where(l => l.UserId == userId);

            if (filter == LoanFilter.Active)
                query = query.Where(l => l.Status == LoanStatus.Active);
            else if (filter == LoanFilter.Overdue)
                query = query.Where(l => l.Status == LoanStatus.Active && l.DueAt < now);
            else if (filter == LoanFilter.History)
                query = query.Where(l => l.Status == LoanStatus.Returned);

            var loans = await query
                .Include(l => l.Book)
                .Include(l => l.Copy)
                .Include(l => l.Fine)
                .OrderByDescending(l => l.BorrowedAt)
                .ToListAsync();

            return loans
                .Select(l => new UserLoanResponse(
                    l.Id,
                    new BookShortResponse(l.Book.Id, l.Book.Title),
                    new CopyShortResponse(l.Copy.Id, l.Copy.InventoryNumber),
                    l.BorrowedAt,
                    l.DueAt,
                    l.ReturnedAt,
                    l.Status.ToApiString(),
                    l.Status == LoanStatus.Active && l.DueAt < now,
                    l.Fine == null ? null : ToFineResponse(l.Fine)))
                .ToList();
        }

        private static Fine? BuildFine(Loan loan, BookCopyCondition condition, DateTime now)
        {
            var amount = 0;
            FineReason? reason = null;

            if (loan.DueAt < now)
            {
                var overdueDays = (int)Math.Ceiling((now - loan.DueAt).TotalDays);
                amount += overdueDays * LibraryRules.OverdueFinePerDay;
                reason = FineReason.Overdue;
            }

            if (condition == BookCopyCondition.Damaged)
            {
                amount += LibraryRules.DamageFineAmount;
                reason = FineReason.Damage;
            }
            else if (condition == BookCopyCondition.Lost)
            {
                amount += LibraryRules.LostFineAmount;
                reason = FineReason.Lost;
            }

            if (reason is null || amount <= 0)
            {
                return null;
            }

            return new Fine
            {
                Loan = loan,
                UserId = loan.UserId,
                Amount = amount,
                Reason = reason.Value,
                Status = FineStatus.Pending,
                CreatedAt = now
            };
        }

        internal static FineResponse ToFineResponse(Fine f) => new(
            f.Id, f.LoanId, f.Amount, f.Reason.ToApiString(), f.Status.ToApiString(), f.CreatedAt, f.PaidAt);
    }
}

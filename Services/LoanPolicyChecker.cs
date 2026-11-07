using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services;

public class LoanPolicyChecker : ILoanPolicyChecker
{
    private readonly AppDbContext _db;

    public LoanPolicyChecker(AppDbContext db)
    {
        _db = db;
    }

    public async Task EnsureNoOverdueAsync(int userId, DateTime now)
    {
        var hasOverdue = await _db.Loans
            .AnyAsync(l => l.UserId == userId && l.ReturnedAt == null && l.DueAt < now);

        if (hasOverdue)
        {
            throw new ConflictException("User has overdue loans");
        }
    }

    public async Task EnsureNoExcessFinesAsync(int userId)
    {
        var pending = await _db.Fines
            .Where(f => f.UserId == userId && f.Status == FineStatus.Pending)
            .SumAsync(f => f.Amount);

        if (pending > LibraryRules.MaxPendingFinesTotal)
        {
            throw new ConflictException(
                $"User has unpaid fines of {pending} RUB (limit {LibraryRules.MaxPendingFinesTotal} RUB)");
        }
    }

    public async Task EnsureCanBorrowAsync(int userId, DateTime now)
    {
        await EnsureNoOverdueAsync(userId, now);
        await EnsureNoExcessFinesAsync(userId);

        var activeLoans = await _db.Loans
            .CountAsync(l => l.UserId == userId && l.ReturnedAt == null);

        if (activeLoans >= LibraryRules.MaxActiveLoans)
        {
            throw new ConflictException("Active loans limit reached");
        }
    }

    public async Task EnsureCanReserveAsync(int userId, DateTime now)
    {
        await EnsureNoOverdueAsync(userId, now);
        await EnsureNoExcessFinesAsync(userId);
    }
}

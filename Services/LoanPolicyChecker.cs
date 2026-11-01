using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
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

    public async Task EnsureCanBorrowAsync(int userId, DateTime now)
    {
        await EnsureNoOverdueAsync(userId, now);

        var activeLoans = await _db.Loans
            .CountAsync(l => l.UserId == userId && l.ReturnedAt == null);

        if (activeLoans >= LibraryRules.MaxActiveLoans)
        {
            throw new ConflictException("Active loans limit reached");
        }
    }
}

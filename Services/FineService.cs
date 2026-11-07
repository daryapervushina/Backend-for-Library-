using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Fines;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services;

public class FineService : IFineService
{
    private readonly AppDbContext _db;

    public FineService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserFinesResponse> GetUserFinesAsync(int userId, int requesterId, bool isLibrarian, FineStatus? status)
    {
        if (!isLibrarian && userId != requesterId)
            throw new ForbiddenException("you don't have permission to access this resource");

        if (!await _db.Users.AnyAsync(u => u.Id == userId))
            throw new NotFoundException("user not found");

        var query = _db.Fines.AsNoTracking().Where(f => f.UserId == userId);

        if (status != null)
            query = query.Where(f => f.Status == status.Value);

        var fines = await query
            .OrderByDescending(f => f.CreatedAt)
            .ThenByDescending(f => f.Id)
            .ToListAsync();

        var debt = await _db.Fines
            .Where(f => f.UserId == userId && f.Status == FineStatus.Pending)
            .SumAsync(f => f.Amount);

        var items = fines
            .Select(f => new FineResponse(
                f.Id, f.LoanId, f.Amount, f.Reason.ToApiString(), f.Status.ToApiString(), f.CreatedAt, f.PaidAt))
            .ToList();

        return new UserFinesResponse(items, debt);
    }

    public async Task<PayFineResponse> PayAsync(int fineId, int userId)
    {
        var fine = await _db.Fines.AsNoTracking().FirstOrDefaultAsync(f => f.Id == fineId);
        if (fine == null)
            throw new NotFoundException("fine not found");

        if (fine.UserId != userId)
            throw new ForbiddenException("you don't have permission to access this resource");

        if (fine.Status != FineStatus.Pending)
            throw new ConflictException($"fine is already {fine.Status.ToApiString()}");

        var now = DateTime.UtcNow;

        var updated = await _db.Fines
            .Where(f => f.Id == fineId && f.Status == FineStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Status, FineStatus.Paid)
                .SetProperty(f => f.PaidAt, (DateTime?)now));

        if (updated == 0)
            throw new ConflictException("fine is already paid or waived");

        return new PayFineResponse(fineId, FineStatus.Paid.ToApiString(), now);
    }
}

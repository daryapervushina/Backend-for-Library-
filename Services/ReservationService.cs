using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Reservations;
using Library.Models;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Library.Services
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _db;
        private readonly IBookLocker _bookLocker;
        private readonly IQueueMaterializer _materializer;
        private readonly IReservationQueue _reservation;
        private readonly ILoanPolicyChecker _policyChecker;

        public ReservationService(AppDbContext db, IReservationQueue reservation,
             IBookLocker bookLocker, IQueueMaterializer materializer, ILoanPolicyChecker policyChecker)
        {
            _db = db;
            _bookLocker = bookLocker;
            _materializer = materializer;
            _reservation = reservation;
            _policyChecker = policyChecker;
        }

        public async Task<ReservationResponse> CreateAsync(int userId, CreateReservationRequest request)
        {
            var now = DateTime.UtcNow;
            var bookId = request.BookId!.Value;

            await _policyChecker.EnsureCanReserveAsync(userId, now);
            await using var tx = await _db.Database.BeginTransactionAsync();

            var book = await _bookLocker.GetForUpdateAsync(bookId);
            await _materializer.MaterializeAsync(book, now);

            var hasAvailableCopy = await _db.BookCopies
                .AnyAsync(c => c.BookId == bookId && c.Status == BookCopyStatus.Available);

            if (hasAvailableCopy)
                throw new ConflictException("the book is available");

            var hasActiveReservation = await _db.Reservations.AnyAsync(r =>
                r.UserId == userId &&
                r.BookId == bookId &&
                (r.Status == ReservationStatus.Waiting || r.Status == ReservationStatus.Ready));

            if (hasActiveReservation)
                throw new ConflictException("user already has an active reservation for this book");

            var reserv = new Reservation
            {
                UserId = userId,
                BookId = book.Id,
                CreatedAt = now,
                Status = ReservationStatus.Waiting
            };
            _db.Reservations.Add(reserv);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return new ReservationResponse(reserv.Id, reserv.BookId, reserv.UserId,
                reserv.CreatedAt, reserv.ExpiresAt, reserv.Status.ToApiString());
        }

        public async Task CancelAsync(int reservationId, int userId, bool isLibrarian)
        {
            var now = DateTime.UtcNow;
            var reservInfo = await _db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservInfo == null)
                throw new NotFoundException("reservation not found");

            if (reservInfo.UserId != userId && !isLibrarian)
                throw new ForbiddenException("you don't have permission to access this resource");

            await using var tx = await _db.Database.BeginTransactionAsync();
            var book = await _bookLocker.GetForUpdateAsync(reservInfo.BookId);
            await _materializer.MaterializeAsync(book, now);
            var reservation = await _db.Reservations
                .Include(r => r.Copy)
                .FirstAsync(r => r.Id == reservationId);

            if (reservation.Status == ReservationStatus.Fulfilled)
                throw new ConflictException("the reservation has already fulfilled");

            if (reservation.Status != ReservationStatus.Waiting && reservation.Status != ReservationStatus.Ready)
                throw new ConflictException("reservation can no longer be cancelled");

            var wasReady = reservation.Status == ReservationStatus.Ready;
            reservation.Status = ReservationStatus.Cancelled;

            if (wasReady && reservation.Copy != null)
            {
                // The held copy goes to the next waiting reader, or becomes AVAILABLE.
                await _reservation.ReleaseCopyAsync(book, reservation.Copy, now);
            }
            else
            {
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
    }
}

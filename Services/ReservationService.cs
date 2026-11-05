using Library.Common;
using Library.Common.Exceptions;
using Library.Data;
using Library.DTOs.Reservations;
using Library.Models;
using Library.Services;
using Library.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Library.Services
{
    public class ReservationService : IReservationService
    {
        private readonly AppDbContext _db;
        private readonly IClock _clock;
        private readonly IBookLocker _bookLocker;
        private readonly IQueueMaterializer _materializer;
        private readonly IReservationQueue _reservation;
        private readonly ILoanPolicyChecker _policyChecker;
        public ReservationService(AppDbContext db, IReservationQueue reservation,
             IClock clock, IBookLocker bookLocker, IQueueMaterializer materializer,ILoanPolicyChecker policyChecker) 
        {
            _db = db;
            _clock = clock;
            _bookLocker = bookLocker;
            _materializer = materializer;
            _reservation = reservation;
            _policyChecker = policyChecker;

        }
        public async Task<ReservationResponse> CreateAsync(int userId, CreateReservationRequest request)
        {
            var now=_clock.UtcNow;
            var bookId = request.BookId!.Value;


            await _policyChecker.EnsureNoOverdueAsync(userId, now);
            await using var tx = await _db.Database.BeginTransactionAsync();

            var book = await _bookLocker.GetForUpdateAsync(bookId);
            await _materializer.MaterializeAsync(book, now);

            if (book.AvailableCopies != 0)
                throw new ConflictException("the book is availible");

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
                reserv.CreatedAt, reserv.ExpiresAt, "WAITING");
        }

        public async Task CancelAsync(int reservationId, int userId, bool isLibrarian)
        {
            var now=_clock.UtcNow;
            var reservInfo= await _db.Reservations.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservInfo == null)
                throw new NotFoundException("reservation not found");

            if (reservInfo.UserId != userId && !isLibrarian)
                throw new ForbiddenException("you don't have permission to access this resource");

            
            await using var tx = await _db.Database.BeginTransactionAsync();
            var book = await _bookLocker.GetForUpdateAsync(reservInfo.BookId);
            await _materializer.MaterializeAsync(book, now);
            var reservation = await _db.Reservations.FirstAsync(r => r.Id == reservationId);
 
            if (reservation.Status == ReservationStatus.Fulfilled)
                throw new ConflictException("the reservation has already fulfilled");

            if(reservation.Status != ReservationStatus.Waiting && reservation.Status != ReservationStatus.Ready)
                throw new ConflictException("reservation can no longer be cancelled");

            if (reservation.Status == ReservationStatus.Ready)
                await _reservation.ReleaseCopyAsync(book, now);
            reservation.Status = ReservationStatus.Cancelled;
            
            await _db.SaveChangesAsync();
            await tx.CommitAsync();

        }

    }

    
}
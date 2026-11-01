using Library.DTOs.Reservations;

namespace Library.Services
{
    public interface IReservationService
    {
        Task<ReservationResponse> CreateAsync(int userId, CreateReservationRequest request);

        Task CancelAsync(int reservationId, int userId, bool isLibrarian);
    }
}
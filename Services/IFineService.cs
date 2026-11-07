using Library.DTOs.Fines;
using Library.Models.Enums;

namespace Library.Services;

public interface IFineService
{
    Task<UserFinesResponse> GetUserFinesAsync(int userId, int requesterId, bool isLibrarian, FineStatus? status);

    Task<PayFineResponse> PayAsync(int fineId, int userId);
}

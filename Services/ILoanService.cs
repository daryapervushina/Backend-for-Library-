using Library.DTOs.Loans;

namespace Library.Services;

public interface ILoanService
{
    Task<LoanResponse> BorrowAsync(int userId, CreateLoanRequest request);

    Task<ReturnLoanResponse> ReturnAsync(int loanId, int userId, bool isLibrarian, string? condition);

    Task<List<UserLoanResponse>> GetUserLoansAsync(int userId, int requesterId, bool isLibrarian, LoanFilter? filter);
}

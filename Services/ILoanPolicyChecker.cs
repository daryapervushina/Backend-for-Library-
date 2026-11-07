namespace Library.Services;

public interface ILoanPolicyChecker
{
    Task EnsureNoOverdueAsync(int userId, DateTime now);

     Task EnsureNoExcessFinesAsync(int userId);

    Task EnsureCanBorrowAsync(int userId, DateTime now);

    Task EnsureCanReserveAsync(int userId, DateTime now);
}

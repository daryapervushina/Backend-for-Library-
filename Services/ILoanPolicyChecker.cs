namespace Library.Services;

public interface ILoanPolicyChecker
{
    Task EnsureNoOverdueAsync(int userId, DateTime now);

    Task EnsureCanBorrowAsync(int userId, DateTime now);
}

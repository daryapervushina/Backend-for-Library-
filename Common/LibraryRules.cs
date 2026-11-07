namespace Library.Common;

public static class LibraryRules
{
    public const int MaxActiveLoans = 5;
    public const int LoanDurationDays = 14;
    public const int ReservationHoldHours = 48;

    public const int OverdueFinePerDay = 10;

    public const int DamageFineAmount = 300;

    public const int LostFineAmount = 1000;

    public const int MaxPendingFinesTotal = 500;
}

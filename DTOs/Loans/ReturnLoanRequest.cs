namespace Library.DTOs.Loans;

public class ReturnLoanRequest
{
    /// <summary>OK (default), DAMAGED or LOST.</summary>
    public string? Condition { get; set; }
}

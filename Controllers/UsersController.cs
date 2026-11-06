using Library.Common;
using Library.DTOs.Loans;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ILoanService _loanService;

    public UsersController(ILoanService loanService)
    {
        _loanService = loanService;
    }

    [HttpGet("{id:int}/loans")]
    [Authorize]
    public async Task<ActionResult<List<UserLoanResponse>>> GetLoans(int id, [FromQuery] LoanFilter? filter)
    {
        int requesterId = User.GetUserId();
        bool isLibrarian = User.IsLibrarian();
        return Ok(await _loanService.GetUserLoansAsync(id, requesterId, isLibrarian, filter));
    }
}

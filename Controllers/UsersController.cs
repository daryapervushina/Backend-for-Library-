using Library.Common;
using Library.DTOs.Fines;
using Library.DTOs.Loans;
using Library.Models.Enums;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ILoanService _loanService;
    private readonly IFineService _fineService;

    public UsersController(ILoanService loanService, IFineService fineService)
    {
        _loanService = loanService;
        _fineService = fineService;
    }

    [HttpGet("{id:int}/loans")]
    [Authorize]
    public async Task<ActionResult<List<UserLoanResponse>>> GetLoans(int id, [FromQuery] LoanFilter? filter)
    {
        int requesterId = User.GetUserId();
        bool isLibrarian = User.IsLibrarian();
        return Ok(await _loanService.GetUserLoansAsync(id, requesterId, isLibrarian, filter));
    }

    [HttpGet("{id:int}/fines")]
    [Authorize]
    public async Task<ActionResult<UserFinesResponse>> GetFines(int id, [FromQuery] FineStatus? status)
    {
        int requesterId = User.GetUserId();
        bool isLibrarian = User.IsLibrarian();
        return Ok(await _fineService.GetUserFinesAsync(id, requesterId, isLibrarian, status));
    }
}

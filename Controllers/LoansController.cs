using Library.DTOs.Auth;
using Library.Filters;
using Library.Services;
using Library.Common;
using Library.DTOs.Loans;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Library.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService)
    {
        _loanService = loanService;
    }
    [Authorize(Roles ="READER")]
    [HttpPost]
    
    public async Task<ActionResult<LoanResponse>> Borrow(CreateLoanRequest request)
    {
        var id = User.GetUserId();
        return Ok(await _loanService.BorrowAsync(id,request));
    }
    [Authorize]
    [HttpPost("{id:int}/return")]
    public async Task<ActionResult<ReturnLoanResponse>> Return(int id)
    {
        bool isLibrarian=User.IsLibrarian();
        int userId=User.GetUserId();
        return Ok(await _loanService.ReturnAsync(id,userId,isLibrarian));
    }
}
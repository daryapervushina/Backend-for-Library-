using Library.Common;
using Library.DTOs.Fines;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/fines")]
public class FinesController : ControllerBase
{
    private readonly IFineService _fineService;

    public FinesController(IFineService fineService)
    {
        _fineService = fineService;
    }

    [HttpPost("{id:int}/pay")]
    [Authorize(Roles = "READER")]
    public async Task<ActionResult<PayFineResponse>> Pay(int id)
    {
        return Ok(await _fineService.PayAsync(id, User.GetUserId()));
    }
}

using Library.DTOs.Copies;
using Library.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers;

[ApiController]
[Route("api/copies")]
public class CopiesController : ControllerBase
{
    private readonly ICopyService _copyService;

    public CopiesController(ICopyService copyService)
    {
        _copyService = copyService;
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<ActionResult<CopyResponse>> Update(int id, UpdateCopyRequest request)
    {
        return Ok(await _copyService.UpdateAsync(id, request));
    }
}

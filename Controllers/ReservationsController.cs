
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Library.DTOs.Reservations;
using Library.Common;
namespace Library.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly _IReservationService _reservationService;
    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }


    [HttpPost]
    [Authorize(Roles ="READER")]
    public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request)
    {
        var userId = User.GetUserId();
        return Ok(await _reservationService.CreateAsync(userId, request));
    }
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<ActionResult> Cancel (int id)
    {
        var userId=User.GetUserId();
        bool isLibrarian = User.IsLibrarian();
        await _reservationService.CancelAsync(id, userId, isLibrarian);
        return Ok();
    }
}
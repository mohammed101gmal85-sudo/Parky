
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Reservation;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Create(
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _reservationService.CreateAsync(
            request,
            userId.Value,
            cancellationToken);

        var response = ApiResponse<ReservationResponse>.SuccessResponse(
            result,
            "Reservation created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationResponse>.FailureResponse(
                    "Reservation was not found."));
        }

        return Ok(
            ApiResponse<ReservationResponse>.SuccessResponse(
                result,
                "Reservation retrieved successfully."));
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationResponse>>>> GetAll(
        [FromQuery] ReservationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationResponse>>.SuccessResponse(
                result,
                "Reservations retrieved successfully."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Update(
        Guid id,
        [FromBody] UpdateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _reservationService.UpdateAsync(
            id,
            userId.Value,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationResponse>.FailureResponse(
                    "Reservation was not found or does not belong to you."));
        }

        return Ok(
            ApiResponse<ReservationResponse>.SuccessResponse(
                result,
                "Reservation updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var request = new CancelReservationRequest
        {
            Reason = "Cancelled by the reservation owner."
        };

        var cancelled = await _reservationService.CancelAsync(
            id,
            userId.Value,
            request,
            cancellationToken);

        if (!cancelled)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Reservation was not found or does not belong to you."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Reservation cancelled successfully."));
    }

    [HttpGet("my-reservations")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationResponse>>>> GetMyReservations(
        [FromQuery] ReservationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await _reservationService.GetMyReservationsAsync(
            userId.Value,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationResponse>>.SuccessResponse(
                result,
                "Your reservations retrieved successfully."));
    }

    [HttpGet("parking/{parkingId:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationResponse>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationResponse>>>> GetByParking(
        Guid parkingId,
        [FromQuery] ReservationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.GetByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationResponse>>.SuccessResponse(
                result,
                "Parking reservations retrieved successfully."));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Cancel(
        Guid id,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var cancelled = await _reservationService.CancelAsync(
            id,
            userId.Value,
            request,
            cancellationToken);

        if (!cancelled)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Reservation was not found or does not belong to you."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Reservation cancelled successfully."));
    }

    [HttpPatch("{id:guid}/confirm")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.ConfirmAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationResponse>.FailureResponse(
                    "Reservation was not found."));
        }

        return Ok(
            ApiResponse<ReservationResponse>.SuccessResponse(
                result,
                "Reservation confirmed successfully."));
    }

    [HttpPatch("{id:guid}/arrival")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> MarkArrival(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.MarkArrivalAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationResponse>.FailureResponse(
                    "Reservation was not found."));
        }

        return Ok(
            ApiResponse<ReservationResponse>.SuccessResponse(
                result,
                "Arrival recorded successfully."));
    }

    [HttpPatch("{id:guid}/departure")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationResponse>>> MarkDeparture(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationService.MarkDepartureAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationResponse>.FailureResponse(
                    "Reservation was not found."));
        }

        return Ok(
            ApiResponse<ReservationResponse>.SuccessResponse(
                result,
                "Departure recorded successfully."));
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }
}


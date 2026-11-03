using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ReservationSpot;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Owner")]
public sealed class ReservationSpotController : ControllerBase
{
    private readonly IReservationSpotService _reservationSpotService;

    public ReservationSpotController(
        IReservationSpotService reservationSpotService)
    {
        _reservationSpotService = reservationSpotService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationSpotResponse>>> Create(
        [FromBody] CreateReservationSpotRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<ReservationSpotResponse>.SuccessResponse(
            result,
            "Reservation spot created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReservationSpotResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationSpotResponse>.FailureResponse(
                    "Reservation spot was not found."));
        }

        return Ok(
            ApiResponse<ReservationSpotResponse>.SuccessResponse(
                result,
                "Reservation spot retrieved successfully."));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationSpotResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationSpotResponse>>>> GetAll(
        [FromQuery] ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationSpotResponse>>.SuccessResponse(
                result,
                "Reservation spots retrieved successfully."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ReservationSpotResponse>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ReservationSpotResponse>>> Update(
        Guid id,
        [FromBody] UpdateReservationSpotRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ReservationSpotResponse>.FailureResponse(
                    "Reservation spot was not found."));
        }

        return Ok(
            ApiResponse<ReservationSpotResponse>.SuccessResponse(
                result,
                "Reservation spot updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _reservationSpotService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Reservation spot was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Reservation spot deleted successfully."));
    }

    [HttpGet("by-reservation/{reservationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationSpotResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationSpotResponse>>>> GetByReservationId(
        Guid reservationId,
        [FromQuery] ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.GetByReservationIdAsync(
            reservationId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationSpotResponse>>.SuccessResponse(
                result,
                "Reservation spots retrieved successfully."));
    }

    [HttpGet("by-parking-spot/{parkingSpotId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationSpotResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ReservationSpotResponse>>>> GetByParkingSpotId(
        Guid parkingSpotId,
        [FromQuery] ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _reservationSpotService.GetByParkingSpotIdAsync(
            parkingSpotId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ReservationSpotResponse>>.SuccessResponse(
                result,
                "Parking spot reservations retrieved successfully."));
    }
}
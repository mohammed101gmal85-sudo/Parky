
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpot;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingSpotController : ControllerBase
{
    private readonly IParkingSpotService _service;

    public ParkingSpotController(IParkingSpotService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingSpotResponse>>>>
        GetAll(
            [FromQuery] ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingSpotResponse>>.SuccessResponse(
                result,
                "Parking spots retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingSpotResponse>>>
        GetById(
            Guid id,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "Parking spot was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotResponse>.SuccessResponse(
                result,
                "Parking spot retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotResponse>>> Create(
        [FromBody] CreateParkingSpotRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "The authenticated user's ID is missing or invalid."));
        }

        var result = await _service.CreateAsync(
            request,
            ownerId,
            cancellationToken);

        var response = ApiResponse<ParkingSpotResponse>.SuccessResponse(
            result,
            "Parking spot created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotResponse>>> Update(
        Guid id,
        [FromBody] UpdateParkingSpotRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "The authenticated user's ID is missing or invalid."));
        }

        var result = await _service.UpdateAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "Parking spot was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotResponse>.SuccessResponse(
                result,
                "Parking spot updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "The authenticated user's ID is missing or invalid."));
        }

        var deleted = await _service.DeleteAsync(
            id,
            ownerId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking spot was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking spot deleted successfully."));
    }

    [HttpGet("by-floor/{parkingFloorId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotResponse>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingSpotResponse>>>>
        GetByParkingFloorId(
            Guid parkingFloorId,
            [FromQuery] ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetByParkingFloorIdAsync(
            parkingFloorId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingSpotResponse>>.SuccessResponse(
                result,
                "Parking floor spots retrieved successfully."));
    }

    [HttpGet("by-floor/{parkingFloorId:guid}/available")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotResponse>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingSpotResponse>>>>
        GetAvailableByParkingFloorId(
            Guid parkingFloorId,
            [FromQuery] ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetAvailableByParkingFloorIdAsync(
            parkingFloorId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingSpotResponse>>.SuccessResponse(
                result,
                "Available parking spots retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotResponse>>>
        UpdateStatus(
            Guid id,
            [FromBody] UpdateParkingSpotStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "The authenticated user's ID is missing or invalid."));
        }

        var result = await _service.UpdateStatusAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ParkingSpotResponse>.FailureResponse(
                    "Parking spot was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotResponse>.SuccessResponse(
                result,
                "Parking spot status updated successfully."));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        ownerId = Guid.Empty;

        var ownerIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(ownerIdValue, out ownerId);
    }
}

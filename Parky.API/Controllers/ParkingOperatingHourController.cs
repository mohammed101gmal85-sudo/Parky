
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingOperatingHour;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingOperatingHourController : ControllerBase
{
    private readonly IParkingOperatingHourService _service;

    public ParkingOperatingHourController(
        IParkingOperatingHourService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingOperatingHourResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingOperatingHourResponse>>>>
        GetAll(
            [FromQuery] ParkingOperatingHourQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingOperatingHourResponse>>
                .SuccessResponse(
                    result,
                    "Parking operating hours retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingOperatingHourResponse>>>
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
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
                    "Parking operating hours were not found."));
        }

        return Ok(
            ApiResponse<ParkingOperatingHourResponse>.SuccessResponse(
                result,
                "Parking operating hours retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingOperatingHourResponse>>>
        Create(
            [FromBody] CreateParkingOperatingHourRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
                    "The authenticated user's ID is missing or invalid."));
        }

        var result = await _service.CreateAsync(
            request,
            ownerId,
            cancellationToken);

        var response =
            ApiResponse<ParkingOperatingHourResponse>.SuccessResponse(
                result,
                "Parking operating hours created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingOperatingHourResponse>>>
        Update(
            Guid id,
            [FromBody] UpdateParkingOperatingHourRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
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
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
                    "Parking operating hours were not found."));
        }

        return Ok(
            ApiResponse<ParkingOperatingHourResponse>.SuccessResponse(
                result,
                "Parking operating hours updated successfully."));
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
                    "Parking operating hours were not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking operating hours deleted successfully."));
    }

    [HttpGet("by-parking/{parkingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<ParkingOperatingHourResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParkingOperatingHourResponse>>>>
        GetByParkingId(
            Guid parkingId,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<ParkingOperatingHourResponse>>
                .SuccessResponse(
                    result,
                    "Parking operating hours retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingOperatingHourResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingOperatingHourResponse>>>
        UpdateStatus(
            Guid id,
            [FromBody] UpdateParkingOperatingHourStatusRequest request,
            CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
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
                ApiResponse<ParkingOperatingHourResponse>.FailureResponse(
                    "Parking operating hours were not found."));
        }

        return Ok(
            ApiResponse<ParkingOperatingHourResponse>.SuccessResponse(
                result,
                "Parking operating hours status updated successfully."));
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

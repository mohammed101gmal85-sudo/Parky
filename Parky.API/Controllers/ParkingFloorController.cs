using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingFloorController : ControllerBase
{
    private readonly IParkingFloorService _service;

    public ParkingFloorController(IParkingFloorService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingFloorResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingFloorResponse>>>> GetAll(
        [FromQuery] ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var floors = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingFloorResponse>>.SuccessResponse(
                floors,
                "Parking floors retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingFloorResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var floor = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (floor is null)
        {
            return NotFound(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Parking floor was not found."));
        }

        return Ok(
            ApiResponse<ParkingFloorResponse>.SuccessResponse(
                floor,
                "Parking floor retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingFloorResponse>>> Create(
        [FromBody] CreateParkingFloorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var floor = await _service.CreateAsync(
            request,
            ownerId,
            cancellationToken);

        var response = ApiResponse<ParkingFloorResponse>.SuccessResponse(
            floor,
            "Parking floor created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = floor.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingFloorResponse>>> Update(
        Guid id,
        [FromBody] UpdateParkingFloorRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var floor = await _service.UpdateAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (floor is null)
        {
            return NotFound(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Parking floor was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingFloorResponse>.SuccessResponse(
                floor,
                "Parking floor updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
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
                    "Unable to identify the authenticated owner."));
        }

        var deleted = await _service.DeactivateAsync(
            id,
            ownerId,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking floor was not found or is already inactive."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking floor deactivated successfully."));
    }

    [HttpGet("by-parking/{parkingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ParkingFloorResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingFloorResponse>>>> GetByParkingId(
        Guid parkingId,
        [FromQuery] ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var floors = await _service.GetByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingFloorResponse>>.SuccessResponse(
                floors,
                "Parking floors retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ParkingFloorResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingFloorResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateParkingFloorStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var floor = await _service.UpdateStatusAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (floor is null)
        {
            return NotFound(
                ApiResponse<ParkingFloorResponse>.FailureResponse(
                    "Parking floor was not found."));
        }

        return Ok(
            ApiResponse<ParkingFloorResponse>.SuccessResponse(
                floor,
                "Parking floor status updated successfully."));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var ownerIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(ownerIdValue, out ownerId);
    }
}
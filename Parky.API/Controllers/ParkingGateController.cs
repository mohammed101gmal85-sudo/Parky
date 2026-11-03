
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingGate;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingGateController : ControllerBase
{
    private readonly IParkingGateService _service;

    public ParkingGateController(IParkingGateService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingGateResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingGateResponse>>>> GetAll(
        [FromQuery] ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var gates = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingGateResponse>>.SuccessResponse(
                gates,
                "Parking gates retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingGateResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var gate = await _service.GetByIdAsync(
            id,
            cancellationToken);

        if (gate is null)
        {
            return NotFound(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Parking gate was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingGateResponse>.SuccessResponse(
                gate,
                "Parking gate retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingGateResponse>>> Create(
        [FromBody] CreateParkingGateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var gate = await _service.CreateAsync(
            request,
            ownerId,
            cancellationToken);

        var response = ApiResponse<ParkingGateResponse>.SuccessResponse(
            gate,
            "Parking gate created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = gate.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingGateResponse>>> Update(
        Guid id,
        [FromBody] UpdateParkingGateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var gate = await _service.UpdateAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (gate is null)
        {
            return NotFound(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Parking gate was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingGateResponse>.SuccessResponse(
                gate,
                "Parking gate updated successfully."));
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
                    "Unable to identify the authenticated owner."));
        }

        var deactivated = await _service.DeactivateAsync(
            id,
            ownerId,
            cancellationToken);

        if (!deactivated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking gate was not found or is already inactive."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking gate deactivated successfully."));
    }

    [HttpGet("by-parking/{parkingId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingGateResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingGateResponse>>>> GetByParkingId(
        Guid parkingId,
        [FromQuery] ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var gates = await _service.GetByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingGateResponse>>.SuccessResponse(
                gates,
                "Parking gates retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingGateResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingGateResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateParkingGateStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return Unauthorized(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var gate = await _service.UpdateStatusAsync(
            id,
            request,
            ownerId,
            cancellationToken);

        if (gate is null)
        {
            return NotFound(
                ApiResponse<ParkingGateResponse>.FailureResponse(
                    "Parking gate was not found."));
        }

        return Ok(
            ApiResponse<ParkingGateResponse>.SuccessResponse(
                gate,
                "Parking gate status updated successfully."));
    }

    private bool TryGetOwnerId(out Guid ownerId)
    {
        var ownerIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(ownerIdValue, out ownerId);
    }
}

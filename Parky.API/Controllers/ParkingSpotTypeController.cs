
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpotType;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingSpotTypeController : ControllerBase
{
    private readonly IParkingSpotTypeService _service;

    public ParkingSpotTypeController(
        IParkingSpotTypeService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingSpotTypeResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingSpotTypeResponse>>>>
        GetAll(
            [FromQuery] ParkingSpotTypeQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingSpotTypeResponse>>.SuccessResponse(
                result,
                "Parking spot types retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingSpotTypeResponse>>>
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
                ApiResponse<ParkingSpotTypeResponse>.FailureResponse(
                    "Parking spot type was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotTypeResponse>.SuccessResponse(
                result,
                "Parking spot type retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotTypeResponse>>>
        Create(
            [FromBody] CreateParkingSpotTypeRequest request,
            CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<ParkingSpotTypeResponse>.SuccessResponse(
            result,
            "Parking spot type created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotTypeResponse>>>
        Update(
            Guid id,
            [FromBody] UpdateParkingSpotTypeRequest request,
            CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ParkingSpotTypeResponse>.FailureResponse(
                    "Parking spot type was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotTypeResponse>.SuccessResponse(
                result,
                "Parking spot type updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
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
        var deleted = await _service.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking spot type was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking spot type deleted successfully."));
    }

    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<IReadOnlyList<ParkingSpotTypeResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ParkingSpotTypeResponse>>>>
        GetActive(CancellationToken cancellationToken)
    {
        var result = await _service.GetActiveAsync(
            cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<ParkingSpotTypeResponse>>.SuccessResponse(
                result,
                "Active parking spot types retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingSpotTypeResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingSpotTypeResponse>>>
        UpdateStatus(
            Guid id,
            [FromBody] UpdateParkingSpotTypeStatusRequest request,
            CancellationToken cancellationToken)
    {
        var result = await _service.UpdateStatusAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<ParkingSpotTypeResponse>.FailureResponse(
                    "Parking spot type was not found."));
        }

        return Ok(
            ApiResponse<ParkingSpotTypeResponse>.SuccessResponse(
                result,
                "Parking spot type status updated successfully."));
    }
}

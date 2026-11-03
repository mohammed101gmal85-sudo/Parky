
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Parking;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ParkingController : ControllerBase
{
    private readonly IParkingService _parkingService;

    public ParkingController(IParkingService parkingService)
    {
        _parkingService = parkingService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingResponse>>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingResponse>>>> GetAll(
        [FromQuery] ParkingQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var parkings = await _parkingService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingResponse>>.SuccessResponse(
                parkings,
                "Parkings retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var parking = await _parkingService.GetByIdAsync(
            id,
            cancellationToken);

        if (parking is null)
        {
            return NotFound(
                ApiResponse<ParkingResponse>.FailureResponse(
                    "Parking was not found or is inactive."));
        }

        return Ok(
            ApiResponse<ParkingResponse>.SuccessResponse(
                parking,
                "Parking retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ParkingResponse>>> Create(
        [FromBody] CreateParkingRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
        {
            return Unauthorized(
                ApiResponse<ParkingResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var parking = await _parkingService.CreateAsync(
            request,
            ownerId.Value,
            cancellationToken);

        var response = ApiResponse<ParkingResponse>.SuccessResponse(
            parking,
            "Parking created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = parking.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<ParkingResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParkingResponse>>> Update(
        Guid id,
        [FromBody] UpdateParkingRequest request,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
        {
            return Unauthorized(
                ApiResponse<ParkingResponse>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var parking = await _parkingService.UpdateAsync(
            id,
            ownerId.Value,
            request,
            cancellationToken);

        if (parking is null)
        {
            return NotFound(
                ApiResponse<ParkingResponse>.FailureResponse(
                    "Parking was not found or does not belong to you."));
        }

        return Ok(
            ApiResponse<ParkingResponse>.SuccessResponse(
                parking,
                "Parking updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var deactivated = await _parkingService.DeactivateAsync(
            id,
            ownerId.Value,
            cancellationToken);

        if (!deactivated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Parking was not found, does not belong to you, or is already inactive."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Parking deactivated successfully."));
    }

    [HttpGet("my-parkings")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<ParkingResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ParkingResponse>>>> GetMyParkings(
        [FromQuery] ParkingQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var ownerId = GetCurrentUserId();

        if (ownerId is null)
        {
            return Unauthorized(
                ApiResponse<PagedResult<ParkingResponse>>.FailureResponse(
                    "Unable to identify the authenticated owner."));
        }

        var parkings = await _parkingService.GetMyParkingsAsync(
            ownerId.Value,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<ParkingResponse>>.SuccessResponse(
                parkings,
                "Your parkings retrieved successfully."));
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


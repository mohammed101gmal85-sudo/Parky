using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Vehicle;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _vehicleService.CreateAsync(
            request,
            userId.Value,
            cancellationToken);

        var response = ApiResponse<VehicleResponse>.SuccessResponse(
            result,
            "Vehicle created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var vehicle = await _vehicleService.GetByIdAsync(
            id,
            cancellationToken);

        if (vehicle is null || vehicle.UserId != userId.Value)
        {
            return NotFound(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Vehicle was not found."));
        }

        return Ok(
            ApiResponse<VehicleResponse>.SuccessResponse(
                vehicle,
                "Vehicle retrieved successfully."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _vehicleService.UpdateAsync(
            id,
            userId.Value,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Vehicle was not found."));
        }

        return Ok(
            ApiResponse<VehicleResponse>.SuccessResponse(
                result,
                "Vehicle updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var deactivated = await _vehicleService.DeactivateAsync(
            id,
            userId.Value,
            cancellationToken);

        if (!deactivated)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Vehicle was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Vehicle deactivated successfully."));
    }

    [HttpGet("my-vehicles")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleResponse>>>> GetMyVehicles(
        [FromQuery] VehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<PagedResult<VehicleResponse>>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var vehicles = await _vehicleService.GetMyVehiclesAsync(
            userId.Value,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<VehicleResponse>>.SuccessResponse(
                vehicles,
                "Your vehicles retrieved successfully."));
    }

    [HttpGet("my-default")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> GetMyDefaultVehicle(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var vehicle = await _vehicleService.GetDefaultByUserIdAsync(
            userId.Value,
            cancellationToken);

        if (vehicle is null)
        {
            return NotFound(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "No active default vehicle was found."));
        }

        return Ok(
            ApiResponse<VehicleResponse>.SuccessResponse(
                vehicle,
                "Default vehicle retrieved successfully."));
    }

    [HttpPatch("my-default")]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VehicleResponse>>> SetMyDefaultVehicle(
        [FromBody] SetDefaultVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        if (request.VehicleId == Guid.Empty)
        {
            return BadRequest(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "VehicleId is required."));
        }

        var vehicle = await _vehicleService.SetDefaultAsync(
            request.VehicleId,
            userId.Value,
            cancellationToken);

        if (vehicle is null)
        {
            return NotFound(
                ApiResponse<VehicleResponse>.FailureResponse(
                    "Active vehicle was not found for your account."));
        }

        return Ok(
            ApiResponse<VehicleResponse>.SuccessResponse(
                vehicle,
                "Default vehicle updated successfully."));
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<VehicleResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<VehicleResponse>>>> GetAll(
        [FromQuery] VehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var vehicles = await _vehicleService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<VehicleResponse>>.SuccessResponse(
                vehicles,
                "Vehicles retrieved successfully."));
    }

    private Guid? GetCurrentUserId()
    {
        var userId =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : null;
    }
}
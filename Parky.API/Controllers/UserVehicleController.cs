using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.UserVehicle;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UserVehicleController : ControllerBase
{
    private readonly IUserVehicleService _userVehicleService;

    public UserVehicleController(
        IUserVehicleService userVehicleService)
    {
        _userVehicleService = userVehicleService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserVehicleResponse>>> Create(
        [FromBody] CreateUserVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        request.UserId = currentUserId.Value;

        var result = await _userVehicleService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<UserVehicleResponse>.SuccessResponse(
            result,
            "User vehicle created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserVehicleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _userVehicleService.GetByIdAsync(
            id,
            cancellationToken);

        if (result is null || result.UserId != currentUserId.Value)
        {
            return NotFound(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "User vehicle was not found."));
        }

        return Ok(
            ApiResponse<UserVehicleResponse>.SuccessResponse(
                result,
                "User vehicle retrieved successfully."));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserVehicleResponse>>> Update(
        Guid id,
        [FromBody] UpdateUserVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var existing = await _userVehicleService.GetByIdAsync(
            id,
            cancellationToken);

        if (existing is null || existing.UserId != currentUserId.Value)
        {
            return NotFound(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "User vehicle was not found."));
        }

        var result = await _userVehicleService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "User vehicle was not found."));
        }

        return Ok(
            ApiResponse<UserVehicleResponse>.SuccessResponse(
                result,
                "User vehicle updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<bool>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var existing = await _userVehicleService.GetByIdAsync(
            id,
            cancellationToken);

        if (existing is null || existing.UserId != currentUserId.Value)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "User vehicle was not found."));
        }

        var deleted = await _userVehicleService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "User vehicle was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "User vehicle deleted successfully."));
    }

    [HttpGet("my-vehicles")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserVehicleResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserVehicleResponse>>>> GetMyVehicles(
        [FromQuery] UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<PagedResult<UserVehicleResponse>>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _userVehicleService.GetByUserIdAsync(
            currentUserId.Value,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<UserVehicleResponse>>.SuccessResponse(
                result,
                "Your vehicles retrieved successfully."));
    }

    [HttpGet("my-default")]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserVehicleResponse>>> GetMyDefaultVehicle(
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _userVehicleService.GetDefaultByUserIdAsync(
            currentUserId.Value,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "No default vehicle is assigned to this user."));
        }

        return Ok(
            ApiResponse<UserVehicleResponse>.SuccessResponse(
                result,
                "Default vehicle retrieved successfully."));
    }

    [HttpPatch("my-vehicles/{vehicleId:guid}/set-default")]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserVehicleResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserVehicleResponse>>> SetDefault(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();

        if (currentUserId is null)
        {
            return Unauthorized(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "Unable to identify the authenticated user."));
        }

        var result = await _userVehicleService.SetDefaultAsync(
            currentUserId.Value,
            vehicleId,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<UserVehicleResponse>.FailureResponse(
                    "This vehicle is not linked to your account."));
        }

        return Ok(
            ApiResponse<UserVehicleResponse>.SuccessResponse(
                result,
                "Default vehicle updated successfully."));
    }

    [HttpGet]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserVehicleResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserVehicleResponse>>>> GetAll(
        [FromQuery] UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _userVehicleService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<UserVehicleResponse>>.SuccessResponse(
                result,
                "User vehicles retrieved successfully."));
    }

    [HttpGet("by-vehicle/{vehicleId:guid}")]
    [Authorize(Roles = "Owner")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserVehicleResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserVehicleResponse>>>> GetByVehicleId(
        Guid vehicleId,
        [FromQuery] UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _userVehicleService.GetByVehicleIdAsync(
            vehicleId,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<UserVehicleResponse>>.SuccessResponse(
                result,
                "User vehicles retrieved successfully."));
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
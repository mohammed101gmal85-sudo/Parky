using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.User;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserController(
        IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserManagementResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserManagementResponse>>>> GetAll(
        [FromQuery] UserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _userManagementService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<UserManagementResponse>>.SuccessResponse(
                result,
                "Users retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserManagementResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserManagementResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            return NotFound(
                ApiResponse<UserManagementResponse>.FailureResponse(
                    "User was not found."));
        }

        return Ok(
            ApiResponse<UserManagementResponse>.SuccessResponse(
                user,
                "User retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<UserManagementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserManagementResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserManagementResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserManagementResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.UpdateStatusAsync(
            id,
            request,
            cancellationToken);

        if (user is null)
        {
            return NotFound(
                ApiResponse<UserManagementResponse>.FailureResponse(
                    "User was not found."));
        }

        var message = request.IsActive
            ? "User activated successfully."
            : "User deactivated successfully.";

        return Ok(
            ApiResponse<UserManagementResponse>.SuccessResponse(
                user,
                message));
    }
}

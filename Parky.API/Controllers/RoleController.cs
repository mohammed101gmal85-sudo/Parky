using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Parky.Application.Common;
using Parky.Application.DTOs.Role;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class RoleController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RoleController(
        IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    // ----------------------------------------------------
    // Get All Roles
    // ----------------------------------------------------

    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RoleManagementResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<object>),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleManagementResponse>>>> GetAll(
        [FromQuery] RoleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.GetAllAsync(
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<RoleManagementResponse>>.SuccessResponse(
                result,
                "Roles retrieved successfully."));
    }

    // ----------------------------------------------------
    // Get Role By Id
    // ----------------------------------------------------

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoleManagementResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var role = await _roleManagementService.GetByIdAsync(
            id,
            cancellationToken);

        if (role is null)
        {
            return NotFound(
                ApiResponse<RoleManagementResponse>.FailureResponse(
                    "Role was not found."));
        }

        return Ok(
            ApiResponse<RoleManagementResponse>.SuccessResponse(
                role,
                "Role retrieved successfully."));
    }

    // ----------------------------------------------------
    // Create Role
    // ----------------------------------------------------

    [HttpPost]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleManagementResponse>>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.CreateAsync(
            request,
            cancellationToken);

        var response = ApiResponse<RoleManagementResponse>.SuccessResponse(
            result,
            "Role created successfully.");

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            response);
    }

    // ----------------------------------------------------
    // Update Role
    // ----------------------------------------------------

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<RoleManagementResponse>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<RoleManagementResponse>>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (result is null)
        {
            return NotFound(
                ApiResponse<RoleManagementResponse>.FailureResponse(
                    "Role was not found."));
        }

        return Ok(
            ApiResponse<RoleManagementResponse>.SuccessResponse(
                result,
                "Role updated successfully."));
    }

    // ----------------------------------------------------
    // Delete Role
    // ----------------------------------------------------

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await _roleManagementService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound(
                ApiResponse<bool>.FailureResponse(
                    "Role was not found."));
        }

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                true,
                "Role deleted successfully."));
    }

    // ----------------------------------------------------
    // Get Users By Role
    // ----------------------------------------------------

    [HttpGet("{id:guid}/users")]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RoleUserResponse>>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RoleUserResponse>>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<PagedResult<RoleUserResponse>>),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoleUserResponse>>>> GetUsersByRole(
        Guid id,
        [FromQuery] RoleUserQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.GetUsersByRoleAsync(
            id,
            parameters,
            cancellationToken);

        return Ok(
            ApiResponse<PagedResult<RoleUserResponse>>.SuccessResponse(
                result,
                "Role users retrieved successfully."));
    }

    // ----------------------------------------------------
    // Assign Role To User
    // ----------------------------------------------------

    [HttpPost("assign-user")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> AssignToUser(
        [FromBody] AssignUserToRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.AssignToUserAsync(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                result,
                "Role assigned to user successfully."));
    }

    // ----------------------------------------------------
    // Remove Role From User
    // ----------------------------------------------------

    [HttpPost("remove-user")]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ApiResponse<bool>),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveFromUser(
        [FromBody] RemoveUserFromRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.RemoveFromUserAsync(
            request,
            cancellationToken);

        return Ok(
            ApiResponse<bool>.SuccessResponse(
                result,
                "Role removed from user successfully."));
    }
}
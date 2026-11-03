using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;
using Parky.Application.DTOs.Role;

namespace Parky.Application.Interfaces.Services;

public interface IRoleManagementService
{
    Task<PagedResult<RoleManagementResponse>> GetAllAsync(
        RoleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<RoleManagementResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RoleManagementResponse> CreateAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<RoleManagementResponse?> UpdateAsync(
        Guid id,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RoleUserResponse>> GetUsersByRoleAsync(
        Guid roleId,
        RoleUserQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> AssignToUserAsync(
        AssignUserToRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFromUserAsync(
        RemoveUserFromRoleRequest request,
        CancellationToken cancellationToken = default);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;
using Parky.Application.DTOs.User;

namespace Parky.Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<PagedResult<UserManagementResponse>> GetAllAsync(
        UserQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<UserManagementResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserManagementResponse?> UpdateStatusAsync(
        Guid id,
        UpdateUserStatusRequest request,
        CancellationToken cancellationToken = default);
}

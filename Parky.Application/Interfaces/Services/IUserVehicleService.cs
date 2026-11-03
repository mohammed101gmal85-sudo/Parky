using Parky.Application.Common;
using Parky.Application.DTOs.UserVehicle;

namespace Parky.Application.Interfaces.Services;

public interface IUserVehicleService
{
    Task<UserVehicleResponse> CreateAsync(
        CreateUserVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<UserVehicleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicleResponse>> GetAllAsync(
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicleResponse>> GetByUserIdAsync(
        Guid userId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicleResponse>> GetByVehicleIdAsync(
        Guid vehicleId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<UserVehicleResponse?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserVehicleResponse?> UpdateAsync(
        Guid id,
        UpdateUserVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserVehicleResponse?> SetDefaultAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}
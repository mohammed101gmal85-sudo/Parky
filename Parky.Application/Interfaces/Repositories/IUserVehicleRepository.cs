using Parky.Application.Common;
using Parky.Application.DTOs.UserVehicle;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IUserVehicleRepository
{
    Task<UserVehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserVehicle>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicle>> GetPagedAsync(
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserVehicle>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicle>> GetPagedByUserIdAsync(
        Guid userId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserVehicle>> GetByVehicleIdAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<UserVehicle>> GetPagedByVehicleIdAsync(
        Guid vehicleId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<UserVehicle?> GetByUserAndVehicleAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<UserVehicle?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default);
}
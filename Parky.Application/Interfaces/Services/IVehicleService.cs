using Parky.Application.Common;
using Parky.Application.DTOs.Vehicle;

namespace Parky.Application.Interfaces.Services;

public interface IVehicleService
{
    Task<VehicleResponse> CreateAsync(
        CreateVehicleRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VehicleResponse>> GetAllAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VehicleResponse>> GetMyVehiclesAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse?> UpdateAsync(
        Guid id,
        Guid userId,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<VehicleResponse?> SetDefaultAsync(
        Guid vehicleId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
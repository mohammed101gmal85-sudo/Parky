using Parky.Application.Common;
using Parky.Application.DTOs.Vehicle;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<Vehicle>> GetPagedAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Vehicle>> GetPagedByUserIdAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vehicle>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Vehicle>> GetPagedActiveByUserIdAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<Vehicle?> GetByPlateNumberAsync(
        string plateNumber,
        string? plateCode,
        CancellationToken cancellationToken = default);

    Task<bool> PlateNumberExistsAsync(
        string plateNumber,
        string? plateCode,
        Guid? excludedVehicleId = null,
        CancellationToken cancellationToken = default);

    Task<Vehicle?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default);
}
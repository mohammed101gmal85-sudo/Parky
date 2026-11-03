
using Parky.Application.Common;
using Parky.Application.DTOs.Parking;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingRepository
{
    Task<Parking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parking>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Parking>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Parking>> GetPagedAsync(
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Parking>> GetPagedByOwnerIdAsync(
        Guid ownerId,
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Parking parking,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Parking parking,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}


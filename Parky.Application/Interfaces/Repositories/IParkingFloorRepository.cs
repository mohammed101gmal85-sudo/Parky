using Parky.Application.Common;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingFloorRepository
{
    Task<ParkingFloor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingFloor>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingFloor>> GetPagedAsync(
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingFloor>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingFloor>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingExistsAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<bool> FloorNumberExistsAsync(
        Guid parkingId,
        int floorNumber,
        Guid? excludedFloorId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingFloor floor,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingFloor floor,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingBelongsToOwnerAsync(
        Guid parkingId,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}
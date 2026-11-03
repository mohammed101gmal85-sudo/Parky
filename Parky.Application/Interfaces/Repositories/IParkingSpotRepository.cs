
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpot;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingSpotRepository
{
    Task<ParkingSpot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpot>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpot>> GetByParkingFloorIdAsync(
        Guid parkingFloorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpot>> GetAvailableByParkingFloorIdAsync(
        Guid parkingFloorId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpot>> GetPagedAsync(
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpot>> GetPagedByParkingFloorIdAsync(
        Guid parkingFloorId,
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpot>> GetPagedAvailableByParkingFloorIdAsync(
        Guid parkingFloorId,
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingFloorExistsAsync(
        Guid parkingFloorId,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingFloorBelongsToOwnerAsync(
        Guid parkingFloorId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingSpotTypeExistsAsync(
        Guid parkingSpotTypeId,
        CancellationToken cancellationToken = default);

    Task<bool> SpotNumberExistsAsync(
        Guid parkingFloorId,
        string spotNumber,
        Guid? excludedSpotId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default);
}

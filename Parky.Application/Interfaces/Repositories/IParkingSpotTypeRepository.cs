
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpotType;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingSpotTypeRepository
{
    Task<ParkingSpotType?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpotType>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpotType>> GetPagedAsync(
        ParkingSpotTypeQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpotType>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasAssignedParkingSpotsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default);
}

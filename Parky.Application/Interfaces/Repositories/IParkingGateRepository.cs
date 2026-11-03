
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingGate;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingGateRepository
{
    Task<ParkingGate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingGate>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingGate>> GetPagedAsync(
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingGate>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingGate>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingGate?> GetByGateCodeAsync(
        string gateCode,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingExistsAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingBelongsToOwnerAsync(
        Guid parkingId,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> GateCodeExistsAsync(
        string gateCode,
        Guid? excludedGateId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingGate gate,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingGate gate,
        CancellationToken cancellationToken = default);
}

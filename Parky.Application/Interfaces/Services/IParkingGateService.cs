
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingGate;

namespace Parky.Application.Interfaces.Services;

public interface IParkingGateService
{
    Task<ParkingGateResponse> CreateAsync(
        CreateParkingGateRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingGateResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingGateResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingGateResponse>> GetAllAsync(
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingGateResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingGateResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingGateResponse?> UpdateAsync(
        Guid id,
        UpdateParkingGateRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingGateResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingGateStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

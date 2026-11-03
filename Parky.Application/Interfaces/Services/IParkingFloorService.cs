using Parky.Application.Common;
using Parky.Application.DTOs.ParkingFloor;

namespace Parky.Application.Interfaces.Services;

public interface IParkingFloorService
{
    Task<ParkingFloorResponse> CreateAsync(
        CreateParkingFloorRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingFloorResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingFloorResponse>> GetAllAsync(
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingFloorResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingFloorResponse?> UpdateAsync(
        Guid id,
        UpdateParkingFloorRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingFloorResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingFloorStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpot;

namespace Parky.Application.Interfaces.Services;

public interface IParkingSpotService
{
    Task<ParkingSpotResponse> CreateAsync(
        CreateParkingSpotRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingSpotResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpotResponse>> GetAllAsync(
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpotResponse>> GetByParkingFloorIdAsync(
        Guid parkingFloorId,
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpotResponse>>
        GetAvailableByParkingFloorIdAsync(
            Guid parkingFloorId,
            ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken = default);

    Task<ParkingSpotResponse?> UpdateAsync(
        Guid id,
        UpdateParkingSpotRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingSpotResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingSpotStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

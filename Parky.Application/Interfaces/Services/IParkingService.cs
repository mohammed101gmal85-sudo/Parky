
using Parky.Application.Common;
using Parky.Application.DTOs.Parking;

namespace Parky.Application.Interfaces.Services;

public interface IParkingService
{
    Task<ParkingResponse> CreateAsync(
        CreateParkingRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingResponse>> GetAllAsync(
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingResponse>> GetMyParkingsAsync(
        Guid ownerId,
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingResponse?> UpdateAsync(
        Guid id,
        Guid ownerId,
        UpdateParkingRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}


using Parky.Application.Common;
using Parky.Application.DTOs.ParkingOperatingHour;

namespace Parky.Application.Interfaces.Services;

public interface IParkingOperatingHourService
{
    Task<ParkingOperatingHourResponse> CreateAsync(
        CreateParkingOperatingHourRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingOperatingHourResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingOperatingHourResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingOperatingHourResponse>> GetAllAsync(
        ParkingOperatingHourQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingOperatingHourResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<ParkingOperatingHourResponse?> UpdateAsync(
        Guid id,
        UpdateParkingOperatingHourRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingOperatingHourResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingOperatingHourStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}

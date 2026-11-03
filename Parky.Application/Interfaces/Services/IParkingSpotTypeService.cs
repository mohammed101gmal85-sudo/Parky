
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpotType;

namespace Parky.Application.Interfaces.Services;

public interface IParkingSpotTypeService
{
    Task<ParkingSpotTypeResponse> CreateAsync(
        CreateParkingSpotTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<ParkingSpotTypeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpotTypeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingSpotTypeResponse>> GetAllAsync(
        ParkingSpotTypeQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingSpotTypeResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<ParkingSpotTypeResponse?> UpdateAsync(
        Guid id,
        UpdateParkingSpotTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<ParkingSpotTypeResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingSpotTypeStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}


using Parky.Application.Common;
using Parky.Application.DTOs.ParkingImage;

namespace Parky.Application.Interfaces.Services;

public interface IParkingImageService
{
    Task<ParkingImageResponse> CreateAsync(
        CreateParkingImageRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingImageResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingImageResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingImageResponse>> GetAllAsync(
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingImageResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingImageResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingImageResponse?> UpdateAsync(
        Guid id,
        UpdateParkingImageRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<ParkingImageResponse?> UpdatePrimaryStatusAsync(
        Guid id,
        UpdateParkingImagePrimaryRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingImageResponse>> ReorderAsync(
        Guid parkingId,
        IReadOnlyList<Guid> imageIds,
        Guid ownerId,
        CancellationToken cancellationToken = default);
}


using Parky.Application.Common;
using Parky.Application.DTOs.ParkingImage;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingImageRepository
{
    Task<ParkingImage?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingImage>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingImage>> GetPagedAsync(
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingImage>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingImage>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingImageQueryParameters parameters,
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

    Task<bool> HasPrimaryImageAsync(
        Guid parkingId,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default);

    Task<bool> DisplayOrderExistsAsync(
        Guid parkingId,
        int displayOrder,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default);
}

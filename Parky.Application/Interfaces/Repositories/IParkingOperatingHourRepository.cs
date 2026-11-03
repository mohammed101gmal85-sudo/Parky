
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingOperatingHour;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingOperatingHourRepository
{
    Task<ParkingOperatingHour?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingOperatingHour>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingOperatingHour>> GetPagedAsync(
        ParkingOperatingHourQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingOperatingHour>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<ParkingOperatingHour?> GetByParkingAndDayAsync(
        Guid parkingId,
        DayOfWeek dayOfWeek,
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

    Task<bool> DayExistsAsync(
        Guid parkingId,
        DayOfWeek dayOfWeek,
        Guid? excludedOperatingHourId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default);
}

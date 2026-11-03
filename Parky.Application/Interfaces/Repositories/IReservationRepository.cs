
using Parky.Application.Common;
using Parky.Application.DTOs.Reservation;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reservation>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Reservation>> GetPagedAsync(
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Reservation>> GetPagedByUserIdAsync(
        Guid userId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Reservation>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByReservationNumberAsync(
        string reservationNumber,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingReservationAsync(
        Guid parkingId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);
}

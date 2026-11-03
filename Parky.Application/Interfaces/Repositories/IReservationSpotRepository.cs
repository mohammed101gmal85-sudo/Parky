using Parky.Application.Common;
using Parky.Application.DTOs.ReservationSpot;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IReservationSpotRepository
{
    Task<ReservationSpot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationSpot>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpot>> GetPagedAsync(
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationSpot>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpot>> GetPagedByReservationIdAsync(
        Guid reservationId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReservationSpot>> GetByParkingSpotIdAsync(
        Guid parkingSpotId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpot>> GetPagedByParkingSpotIdAsync(
        Guid parkingSpotId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid reservationId,
        Guid parkingSpotId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default);
}
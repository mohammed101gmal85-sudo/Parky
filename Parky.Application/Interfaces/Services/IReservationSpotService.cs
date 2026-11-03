using Parky.Application.Common;
using Parky.Application.DTOs.ReservationSpot;

namespace Parky.Application.Interfaces.Services;

public interface IReservationSpotService
{
    Task<ReservationSpotResponse> CreateAsync(
        CreateReservationSpotRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationSpotResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpotResponse>> GetAllAsync(
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpotResponse>> GetByReservationIdAsync(
        Guid reservationId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationSpotResponse>> GetByParkingSpotIdAsync(
        Guid parkingSpotId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ReservationSpotResponse?> UpdateAsync(
        Guid id,
        UpdateReservationSpotRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
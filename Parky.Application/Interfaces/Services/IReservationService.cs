
using Parky.Application.Common;
using Parky.Application.DTOs.Reservation;

namespace Parky.Application.Interfaces.Services;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(
        CreateReservationRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetAllAsync(
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetMyReservationsAsync(
        Guid userId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReservationResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse?> UpdateAsync(
        Guid id,
        Guid userId,
        UpdateReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CancelAsync(
        Guid id,
        Guid userId,
        CancelReservationRequest request,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse?> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse?> MarkArrivalAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ReservationResponse?> MarkDepartureAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

using Parky.Application.Common;
using Parky.Application.DTOs.Payment;

namespace Parky.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PaymentResponse>> GetAllAsync(
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);


    Task<PagedResult<PaymentResponse>> GetByReservationIdAsync(
        Guid reservationId,
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default);


    Task<PaymentResponse?> UpdateAsync(
        Guid id,
        UpdatePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentResponse?> UpdateStatusAsync(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
using Parky.Application.Common;
using Parky.Application.DTOs.Payment;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<Payment>> GetPagedAsync(
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default);


    Task<PagedResult<Payment>> GetPagedByReservationIdAsync(
        Guid reservationId,
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<Payment>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetByPaymentNumberAsync(
        string paymentNumber,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> PaymentNumberExistsAsync(
        string paymentNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IdempotencyKeyExistsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Payment payment,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Payment payment,
        CancellationToken cancellationToken = default);
}
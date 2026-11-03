using Parky.Application.Common;
using Parky.Application.DTOs.Invoice;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<Invoice>> GetPagedAsync(
        InvoiceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Invoice>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<Invoice?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    Task<bool> InvoiceNumberExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);
}
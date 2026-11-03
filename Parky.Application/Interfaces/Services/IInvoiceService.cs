using Parky.Application.Common;
using Parky.Application.DTOs.Invoice;

namespace Parky.Application.Interfaces.Services;

public interface IInvoiceService
{
    Task<InvoiceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<InvoiceResponse>> GetAllAsync(
        InvoiceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvoiceResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default);

    Task<InvoiceResponse> UpdateStatusAsync(
        Guid id,
        UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
using AutoMapper;
using Parky.Application.DTOs.Invoice;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Application.Common;

namespace Parky.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMapper _mapper;

    public InvoiceService(
        IInvoiceRepository invoiceRepository,
        IReservationRepository reservationRepository,
        IPaymentRepository paymentRepository,
        IMapper mapper)
    {
        _invoiceRepository = invoiceRepository;
        _reservationRepository = reservationRepository;
        _paymentRepository = paymentRepository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Get Invoice By Id
    // ----------------------------------------------------

    public async Task<InvoiceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(
            id,
            cancellationToken);

        return invoice is null
            ? null
            : MapToResponse(invoice);
    }

    // ----------------------------------------------------
    // Get All Invoices
    // ----------------------------------------------------

    public async Task<PagedResult<InvoiceResponse>> GetAllAsync(
        InvoiceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateQueryParameters(parameters);

        var pagedInvoices = await _invoiceRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(pagedInvoices);
    }

    // ----------------------------------------------------
    // Get Invoices By Reservation Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<InvoiceResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation ID is required.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            reservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException("Reservation was not found.");
        }

        var invoices = await _invoiceRepository.GetByReservationIdAsync(
            reservationId,
            cancellationToken);

        return _mapper.Map<List<InvoiceResponse>>(invoices);
    }

    // ----------------------------------------------------
    // Create Invoice
    // ----------------------------------------------------

    public async Task<InvoiceResponse> CreateAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ReservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation ID is required.");
        }

        ValidateFinancialAmounts(
            request.Subtotal,
            request.DiscountAmount,
            request.TaxAmount);

        var currency = ValidateCurrency(request.Currency);

        var reservation = await _reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException("Reservation was not found.");
        }

        if (await _invoiceRepository.ExistsForReservationAsync(
                request.ReservationId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "An invoice already exists for this reservation.");
        }

        if (request.PaymentId.HasValue)
        {
            if (request.PaymentId.Value == Guid.Empty)
            {
                throw new ArgumentException("Payment ID is invalid.");
            }

            var payment = await _paymentRepository.GetByIdAsync(
                request.PaymentId.Value,
                cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException("Payment was not found.");
            }
        }

        var totalAmount =
            request.Subtotal - request.DiscountAmount + request.TaxAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ReservationId = request.ReservationId,
            PaymentId = request.PaymentId,
            InvoiceNumber = await GenerateInvoiceNumberAsync(
                cancellationToken),
            IssuedAt = DateTimeOffset.UtcNow,
            DueAt = request.DueAt,
            Subtotal = request.Subtotal,
            DiscountAmount = request.DiscountAmount,
            TaxAmount = request.TaxAmount,
            TotalAmount = totalAmount,
            Currency = currency,
            //InvoiceStatus = "Issued",
            InvoiceStatus = "Draft",
            Notes = NormalizeNotes(request.Notes)
        };

        await _invoiceRepository.AddAsync(
            invoice,
            cancellationToken);

        return MapToResponse(invoice);
    }

    // ----------------------------------------------------
    // Update Invoice
    // ----------------------------------------------------

    public async Task<InvoiceResponse> UpdateAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoice = await _invoiceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        if (invoice.InvoiceStatus != "Draft")
        {
            throw new InvalidOperationException(
                "Only draft invoices can be edited.");
        }

        ValidateFinancialAmounts(
            request.Subtotal,
            request.DiscountAmount,
            request.TaxAmount);

        var currency = ValidateCurrency(request.Currency);

        if (request.PaymentId.HasValue)
        {
            if (request.PaymentId.Value == Guid.Empty)
            {
                throw new ArgumentException("Payment ID is invalid.");
            }

            var payment = await _paymentRepository.GetByIdAsync(
                request.PaymentId.Value,
                cancellationToken);

            if (payment is null)
            {
                throw new KeyNotFoundException("Payment was not found.");
            }
        }

        invoice.PaymentId = request.PaymentId;
        invoice.DueAt = request.DueAt;
        invoice.Subtotal = request.Subtotal;
        invoice.DiscountAmount = request.DiscountAmount;
        invoice.TaxAmount = request.TaxAmount;
        invoice.TotalAmount =
            request.Subtotal - request.DiscountAmount + request.TaxAmount;
        invoice.Currency = currency;
        invoice.Notes = NormalizeNotes(request.Notes);

        await _invoiceRepository.UpdateAsync(
            invoice,
            cancellationToken);

        return MapToResponse(invoice);
    }

    // ----------------------------------------------------
    // Update Invoice Status
    // ----------------------------------------------------

    public async Task<InvoiceResponse> UpdateStatusAsync(
        Guid id,
        UpdateInvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.InvoiceStatus))
        {
            throw new ArgumentException("Invoice status is required.");
        }

        var invoice = await _invoiceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        var newStatus = request.InvoiceStatus.Trim();

        var allowedStatuses = new[]
        {
            "Draft",
            "Issued",
            "Paid",
            "Overdue",
            "Cancelled"
        };

        if (!allowedStatuses.Contains(
                newStatus,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Invalid invoice status.");
        }

        var normalizedStatus = allowedStatuses.First(
            x => x.Equals(
                newStatus,
                StringComparison.OrdinalIgnoreCase));

        if (!IsValidStatusTransition(
                invoice.InvoiceStatus,
                normalizedStatus))
        {
            throw new InvalidOperationException(
                $"Invoice status cannot be changed from " +
                $"'{invoice.InvoiceStatus}' to '{normalizedStatus}'.");
        }

        invoice.InvoiceStatus = normalizedStatus;

        await _invoiceRepository.UpdateAsync(
            invoice,
            cancellationToken);

        return MapToResponse(invoice);
    }

    // ----------------------------------------------------
    // Delete Invoice
    // ----------------------------------------------------

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        if (invoice.InvoiceStatus != "Draft")
        {
            throw new InvalidOperationException(
                "Only draft invoices can be deleted.");
        }

        await _invoiceRepository.DeleteAsync(
            invoice,
            cancellationToken);
    }

    // ----------------------------------------------------
    // Generate Invoice Number
    // ----------------------------------------------------

    private async Task<string> GenerateInvoiceNumberAsync(
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var invoiceNumber =
                $"INV-{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"
                .Substring(0, 21)
                .ToUpperInvariant();

            var exists = await _invoiceRepository.InvoiceNumberExistsAsync(
                invoiceNumber,
                cancellationToken);

            if (!exists)
            {
                return invoiceNumber;
            }
        }

        throw new InvalidOperationException(
            "Could not generate a unique invoice number.");
    }

    // ----------------------------------------------------
    // Validate Financial Amounts
    // ----------------------------------------------------

    private static void ValidateFinancialAmounts(
        decimal subtotal,
        decimal discountAmount,
        decimal taxAmount)
    {
        if (subtotal < 0)
        {
            throw new ArgumentException(
                "Subtotal cannot be negative.");
        }

        if (discountAmount < 0)
        {
            throw new ArgumentException(
                "Discount amount cannot be negative.");
        }

        if (taxAmount < 0)
        {
            throw new ArgumentException(
                "Tax amount cannot be negative.");
        }

        if (discountAmount > subtotal)
        {
            throw new ArgumentException(
                "Discount cannot exceed subtotal.");
        }

        if (subtotal - discountAmount + taxAmount < 0)
        {
            throw new ArgumentException(
                "Invoice total cannot be negative.");
        }
    }

    // ----------------------------------------------------
    // Validate Currency
    // ----------------------------------------------------

    private static string ValidateCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.");
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != 3)
        {
            throw new ArgumentException(
                "Currency must be a three-character code, such as EGP.");
        }

        return normalizedCurrency;
    }

    // ----------------------------------------------------
    // Normalize Notes
    // ----------------------------------------------------

    private static string? NormalizeNotes(string? notes)
    {
        return string.IsNullOrWhiteSpace(notes)
            ? null
            : notes.Trim();
    }

    // ----------------------------------------------------
    // Validate Invoice Status Transition
    // ----------------------------------------------------

    private static bool IsValidStatusTransition(
        string currentStatus,
        string newStatus)
    {
        if (currentStatus == newStatus)
        {
            return true;
        }

        return currentStatus switch
        {
            "Draft" => newStatus is "Issued" or "Cancelled",
            "Issued" => newStatus is "Paid" or "Overdue" or "Cancelled",
            "Overdue" => newStatus is "Paid" or "Cancelled",
            "Paid" => false,
            "Cancelled" => false,
            _ => false
        };
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        InvoiceQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.ReservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID cannot be empty.");
        }

        if (parameters.PaymentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Payment ID cannot be empty.");
        }

        if (parameters.MinTotalAmount.HasValue
            && parameters.MinTotalAmount.Value < 0)
        {
            throw new ArgumentException(
                "Minimum total amount cannot be negative.");
        }

        if (parameters.MaxTotalAmount.HasValue
            && parameters.MaxTotalAmount.Value < 0)
        {
            throw new ArgumentException(
                "Maximum total amount cannot be negative.");
        }

        if (parameters.MinTotalAmount.HasValue
            && parameters.MaxTotalAmount.HasValue
            && parameters.MinTotalAmount.Value
                > parameters.MaxTotalAmount.Value)
        {
            throw new ArgumentException(
                "Minimum total amount cannot be greater than maximum total amount.");
        }

        if (parameters.IssuedFrom.HasValue
            && parameters.IssuedTo.HasValue
            && parameters.IssuedFrom.Value > parameters.IssuedTo.Value)
        {
            throw new ArgumentException(
                "Issued from date cannot be greater than issued to date.");
        }

        if (parameters.DueFrom.HasValue
            && parameters.DueTo.HasValue
            && parameters.DueFrom.Value > parameters.DueTo.Value)
        {
            throw new ArgumentException(
                "Due from date cannot be greater than due to date.");
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private InvoiceResponse MapToResponse(Invoice invoice)
    {
        return _mapper.Map<InvoiceResponse>(invoice);
    }

    private PagedResult<InvoiceResponse> MapToPagedResponse(
        PagedResult<Invoice> result)
    {
        return new PagedResult<InvoiceResponse>
        {
            Items = _mapper.Map<List<InvoiceResponse>>(result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
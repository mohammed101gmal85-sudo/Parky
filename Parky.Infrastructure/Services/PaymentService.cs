using AutoMapper;
using Parky.Application.DTOs.Payment;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Application.Common;

namespace Parky.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly INotificationDispatcher _notificationDispatcher;
    private static readonly HashSet<string> AllowedPaymentMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Cash",
            "Card",
            "Wallet",
            "BankTransfer",
            "Online"
        };

    private static readonly HashSet<string> AllowedPaymentStatuses =
        new(StringComparer.Ordinal)
        {
            "Pending",
            "Processing",
            "Succeeded",
            "Failed",
            "Cancelled",
            "Refunded",
            "PartiallyRefunded"
        };

    private readonly IPaymentRepository _paymentRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IMapper _mapper;

    public PaymentService(
    IPaymentRepository paymentRepository,
    IReservationRepository reservationRepository,
    IMapper mapper,
    INotificationDispatcher notificationDispatcher)
    {
        _paymentRepository = paymentRepository;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _notificationDispatcher = notificationDispatcher;
    }

    // ----------------------------------------------------
    // Create Payment
    // ----------------------------------------------------

    public async Task<PaymentResponse> CreateAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var reservation = await _reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException("Reservation was not found.");
        }

        var paymentNumber = request.PaymentNumber.Trim();

        if (await _paymentRepository.PaymentNumberExistsAsync(
                paymentNumber,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "Payment number already exists.");
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        if (idempotencyKey is not null)
        {
            var existingPayment =
                await _paymentRepository.GetByIdempotencyKeyAsync(
                    idempotencyKey,
                    cancellationToken);

            if (existingPayment is not null)
            {
                if (existingPayment.ReservationId != request.ReservationId
                    || existingPayment.Amount != request.Amount
                    || !string.Equals(
                        existingPayment.Currency,
                        request.Currency.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Idempotency key was already used with different payment data.");
                }

                return MapToResponse(existingPayment);
            }
        }

        var payment = new Payment
        {
            ReservationId = request.ReservationId,
            PaymentNumber = paymentNumber,
            Amount = request.Amount,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            PaymentMethod = request.PaymentMethod.Trim(),
            PaymentStatus = "Pending",
            PaymentProvider = NormalizeOptional(request.PaymentProvider),
            IdempotencyKey = idempotencyKey,
            RefundedAmount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _paymentRepository.AddAsync(
            payment,
            cancellationToken);
        await _notificationDispatcher.DispatchAsync(
            reservation.UserId,
            "Payment Request Created",
            $"A payment request for reservation {payment.ReservationId} has been created. Current status: Pending.",
            "Payment",
            payment.Id.ToString(),
            cancellationToken: cancellationToken);

        return MapToResponse(payment);
    }

    // ----------------------------------------------------
    // Get Payment By Id
    // ----------------------------------------------------

    public async Task<PaymentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(
            id,
            cancellationToken);

        return payment is null
            ? null
            : MapToResponse(payment);
    }

    // ----------------------------------------------------
    // Get All Payments With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<PaymentResponse>> GetAllAsync(
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidateQueryParameters(parameters);

        var pagedPayments = await _paymentRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(pagedPayments);
    }

    // ----------------------------------------------------
    // Get Payments By Reservation Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<PaymentResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID is required.",
                nameof(reservationId));
        }

        var payments = await _paymentRepository.GetByReservationIdAsync(
            reservationId,
            cancellationToken);

        return _mapper.Map<List<PaymentResponse>>(payments);
    }

    // ----------------------------------------------------
    // Get Payments By Reservation Id With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<PaymentResponse>> GetByReservationIdAsync(
        Guid reservationId,
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID is required.",
                nameof(reservationId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        if (parameters.ReservationId.HasValue &&
            parameters.ReservationId.Value != reservationId)
        {
            throw new ArgumentException(
                "The query ReservationId must match the route ReservationId.");
        }

        var pagedPayments =
            await _paymentRepository.GetPagedByReservationIdAsync(
                reservationId,
                parameters,
                cancellationToken);

        return MapToPagedResponse(pagedPayments);
    }

    // ----------------------------------------------------
    // Update Payment
    // ----------------------------------------------------

    public async Task<PaymentResponse?> UpdateAsync(
        Guid id,
        UpdatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateUpdateRequest(request);

        var payment = await _paymentRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (payment is null)
        {
            return null;
        }

        if (payment.PaymentStatus != "Pending")
        {
            throw new InvalidOperationException(
                "Only pending payments can have their payment details updated.");
        }

        payment.PaymentMethod = request.PaymentMethod.Trim();
        payment.PaymentProvider = NormalizeOptional(request.PaymentProvider);
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        await _paymentRepository.UpdateAsync(
            payment,
            cancellationToken);

        return MapToResponse(payment);
    }

    // ----------------------------------------------------
    // Update Payment Status
    // ----------------------------------------------------

    public async Task<PaymentResponse?> UpdateStatusAsync(
        Guid id,
        UpdatePaymentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateStatusRequest(request);

        var payment = await _paymentRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (payment is null)
        {
            return null;
        }

        var newStatus = request.PaymentStatus.Trim();

        if (payment.PaymentStatus == newStatus)
        {
            return MapToResponse(payment);
        }

        if (!IsAllowedTransition(payment.PaymentStatus, newStatus))
        {
            throw new InvalidOperationException(
                $"Payment status cannot change from {payment.PaymentStatus} to {newStatus}.");
        }

        if (newStatus == "Succeeded")
        {
            throw new InvalidOperationException(
                "Payment success must be confirmed through a trusted payment-provider integration.");
        }

        if (newStatus is "Refunded" or "PartiallyRefunded")
        {
            throw new InvalidOperationException(
                "Refund statuses must be set only after a refund has been processed and verified.");
        }

        payment.PaymentStatus = newStatus;
        payment.UpdatedAt = DateTimeOffset.UtcNow;

        if (newStatus == "Failed")
        {
            payment.FailedAt = DateTimeOffset.UtcNow;
            payment.FailureReason =
                NormalizeOptional(request.FailureReason);
        }

        if (!string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            var transactionId = request.ProviderTransactionId.Trim();

            if (!string.IsNullOrWhiteSpace(payment.ProviderTransactionId)
                && payment.ProviderTransactionId != transactionId)
            {
                throw new InvalidOperationException(
                    "Provider transaction ID cannot be replaced.");
            }

            payment.ProviderTransactionId = transactionId;
        }

        await _paymentRepository.UpdateAsync(
    payment,
    cancellationToken);

        var reservation = await _reservationRepository.GetByIdAsync(
            payment.ReservationId,
            cancellationToken);

        if (reservation is not null)
        {
            var notificationTitle = newStatus switch
            {
                "Processing" => "Payment Processing",
                "Failed" => "Payment Failed",
                "Cancelled" => "Payment Cancelled",
                _ => null
            };

            var notificationMessage = newStatus switch
            {
                "Processing" =>
                    $"Payment for reservation {reservation.ReservationNumber} is being processed.",

                "Failed" =>
                    $"Payment for reservation {reservation.ReservationNumber} has failed. Reason: {payment.FailureReason ?? "Not specified"}.",

                "Cancelled" =>
                    $"Payment for reservation {reservation.ReservationNumber} has been cancelled.",

                _ => null
            };

            if (notificationTitle is not null &&
                notificationMessage is not null)
            {
                await _notificationDispatcher.DispatchAsync(
                    reservation.UserId,
                    notificationTitle,
                    notificationMessage,
                    "Payment",
                    payment.Id.ToString(),
                    cancellationToken: cancellationToken);
            }
        }

        return MapToResponse(payment);
    }

    // ----------------------------------------------------
    // Delete Payment
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (payment is null)
        {
            return false;
        }

        if (payment.PaymentStatus != "Pending"
            && payment.PaymentStatus != "Failed"
            && payment.PaymentStatus != "Cancelled")
        {
            throw new InvalidOperationException(
                "Processed payments cannot be deleted.");
        }

        await _paymentRepository.DeleteAsync(
            payment,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Validate Create Request
    // ----------------------------------------------------

    private static void ValidateCreateRequest(
        CreatePaymentRequest request)
    {
        if (request.ReservationId == Guid.Empty)
        {
            throw new ArgumentException("Reservation ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentNumber)
            || request.PaymentNumber.Trim().Length > 100)
        {
            throw new ArgumentException("Invalid payment number.");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException(
                "Payment amount must be greater than zero.");
        }

        if (request.Amount > 1_000_000_000m)
        {
            throw new ArgumentException("Payment amount is too large.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency)
            || request.Currency.Trim().Length != 3
            || request.Currency.Trim().Any(c => c < 'A' || c > 'Z'))
        {
            throw new ArgumentException(
                "Currency must be a three-letter uppercase currency code.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethod)
            || request.PaymentMethod.Trim().Length > 30
            || !AllowedPaymentMethods.Contains(request.PaymentMethod.Trim()))
        {
            throw new ArgumentException("Unsupported payment method.");
        }

        if (request.PaymentProvider?.Length > 100)
        {
            throw new ArgumentException("Payment provider is too long.");
        }

        if (request.IdempotencyKey?.Length > 200)
        {
            throw new ArgumentException("Idempotency key is too long.");
        }
    }

    // ----------------------------------------------------
    // Validate Update Request
    // ----------------------------------------------------

    private static void ValidateUpdateRequest(
        UpdatePaymentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod)
            || request.PaymentMethod.Trim().Length > 30
            || !AllowedPaymentMethods.Contains(request.PaymentMethod.Trim()))
        {
            throw new ArgumentException("Unsupported payment method.");
        }

        if (request.PaymentProvider?.Length > 100)
        {
            throw new ArgumentException("Payment provider is too long.");
        }
    }

    // ----------------------------------------------------
    // Validate Status Request
    // ----------------------------------------------------

    private static void ValidateStatusRequest(
        UpdatePaymentStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentStatus)
            || !AllowedPaymentStatuses.Contains(request.PaymentStatus.Trim()))
        {
            throw new ArgumentException("Invalid payment status.");
        }

        if (request.ProviderTransactionId?.Length > 200)
        {
            throw new ArgumentException("Provider transaction ID is too long.");
        }

        if (request.FailureReason?.Length > 500)
        {
            throw new ArgumentException("Failure reason is too long.");
        }
    }

    // ----------------------------------------------------
    // Validate Allowed Status Transition
    // ----------------------------------------------------

    private static bool IsAllowedTransition(
        string currentStatus,
        string newStatus)
    {
        return currentStatus switch
        {
            "Pending" => newStatus is "Processing" or "Failed" or "Cancelled",
            "Processing" => newStatus is "Failed" or "Cancelled",
            _ => false
        };
    }

    // ----------------------------------------------------
    // Normalize Optional Values
    // ----------------------------------------------------

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        PaymentQueryParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.ReservationId.HasValue &&
            parameters.ReservationId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID cannot be empty.");
        }

        if (!string.IsNullOrWhiteSpace(parameters.PaymentMethod))
        {
            var paymentMethod = parameters.PaymentMethod.Trim();

            if (paymentMethod.Length > 30 ||
                !AllowedPaymentMethods.Contains(paymentMethod))
            {
                throw new ArgumentException(
                    "Unsupported payment method filter.");
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.PaymentStatus))
        {
            var paymentStatus = parameters.PaymentStatus.Trim();

            if (!AllowedPaymentStatuses.Any(status =>
                    string.Equals(
                        status,
                        paymentStatus,
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    "Unsupported payment status filter.");
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.Currency))
        {
            var currency = parameters.Currency.Trim();

            if (currency.Length != 3 ||
                currency.Any(character =>
                    !char.IsAsciiLetter(character)))
            {
                throw new ArgumentException(
                    "Currency must be a three-letter currency code.");
            }
        }

        if (parameters.MinAmount.HasValue &&
            parameters.MinAmount.Value < 0)
        {
            throw new ArgumentException(
                "Minimum amount cannot be negative.");
        }

        if (parameters.MaxAmount.HasValue &&
            parameters.MaxAmount.Value < 0)
        {
            throw new ArgumentException(
                "Maximum amount cannot be negative.");
        }

        if (parameters.MinAmount.HasValue &&
            parameters.MaxAmount.HasValue &&
            parameters.MinAmount.Value > parameters.MaxAmount.Value)
        {
            throw new ArgumentException(
                "Minimum amount cannot be greater than maximum amount.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "Created from date cannot be greater than created to date.");
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private PaymentResponse MapToResponse(Payment payment)
    {
        return _mapper.Map<PaymentResponse>(payment);
    }

    private PagedResult<PaymentResponse> MapToPagedResponse(
        PagedResult<Payment> result)
    {
        return new PagedResult<PaymentResponse>
        {
            Items = _mapper.Map<List<PaymentResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.QRCode;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class QRCodeService : IQRCodeService
{
    private readonly IQRCodeRepository _qrCodeRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IMapper _mapper;
    private readonly INotificationDispatcher _notificationDispatcher;
    public QRCodeService(
    IQRCodeRepository qrCodeRepository,
    IReservationRepository reservationRepository,
    IMapper mapper,
    INotificationDispatcher notificationDispatcher)
    {
        _qrCodeRepository = qrCodeRepository;
        _reservationRepository = reservationRepository;
        _mapper = mapper;
        _notificationDispatcher = notificationDispatcher;
    }

    // ----------------------------------------------------
    // Create QR Code
    // ----------------------------------------------------

    public async Task<CreatedQRCodeResponse> CreateAsync(
        CreateQRCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ReservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "ReservationId is required.");
        }

        if (request.ValidityMinutes < 1 ||
            request.ValidityMinutes > 720)
        {
            throw new ArgumentException(
                "ValidityMinutes must be between 1 and 720.");
        }

        var reservation = await _reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);

        if (reservation is null)
        {
            throw new KeyNotFoundException(
                "Reservation was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var hasActiveCode =
            await _qrCodeRepository.HasActiveCodeForReservationAsync(
                request.ReservationId,
                now,
                cancellationToken);

        if (hasActiveCode)
        {
            throw new InvalidOperationException(
                "This reservation already has an active QR code.");
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);

        var token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var tokenHash = ComputeTokenHash(token);

        var qrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = request.ReservationId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(request.ValidityMinutes),
            UsedAt = null,
            IsRevoked = false,
            UsageCount = 0
        };

        await _qrCodeRepository.AddAsync(
            qrCode,
            cancellationToken);

        await _notificationDispatcher.DispatchAsync(
          reservation.UserId,
          "QR Code Generated",
          $"A QR code has been generated for reservation {reservation.ReservationNumber}. You can use it for parking entry.",
          "QRCode",
          qrCode.Id.ToString(),
          cancellationToken: cancellationToken);

        return new CreatedQRCodeResponse
        {
            QRCode = MapToResponse(qrCode),
            Token = token
        };
    }

    // ----------------------------------------------------
    // Get QR Code By Id
    // ----------------------------------------------------

    public async Task<QRCodeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var qrCode = await _qrCodeRepository.GetByIdAsync(
            id,
            cancellationToken);

        return qrCode is null
            ? null
            : MapToResponse(qrCode);
    }

    // ----------------------------------------------------
    // Get QR Codes By Reservation Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<QRCodeResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID is invalid.");
        }

        var qrCodes = await _qrCodeRepository.GetByReservationIdAsync(
            reservationId,
            cancellationToken);

        return _mapper.Map<List<QRCodeResponse>>(qrCodes);
    }

    // ----------------------------------------------------
    // Get QR Codes By Reservation Id With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<QRCodeResponse>> GetPagedByReservationIdAsync(
        Guid reservationId,
        QRCodeQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (reservationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Reservation ID is invalid.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _qrCodeRepository.GetPagedByReservationIdAsync(
            reservationId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Validate And Use QR Code
    // ----------------------------------------------------

    public async Task<QRCodeResponse?> ValidateAndUseAsync(
        ValidateQRCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return null;
        }

        var tokenHash = ComputeTokenHash(request.Token);

        var qrCode = await _qrCodeRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (qrCode is null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        if (qrCode.IsRevoked ||
            qrCode.ExpiresAt <= now ||
            qrCode.UsedAt is not null)
        {
            return null;
        }

        qrCode.UsedAt = now;
        qrCode.UsageCount++;

        await _qrCodeRepository.UpdateAsync(
            qrCode,
            cancellationToken);

        var reservation = await _reservationRepository.GetByIdAsync(
            qrCode.ReservationId,
            cancellationToken);

        if (reservation is not null)
        {
            await _notificationDispatcher.DispatchAsync(
                reservation.UserId,
                "QR Code Used",
                $"Your QR code for reservation {reservation.ReservationNumber} was validated and used successfully.",
                "QRCode",
                qrCode.Id.ToString(),
                cancellationToken: cancellationToken);
        }

        return MapToResponse(qrCode);
    }

    // ----------------------------------------------------
    // Revoke QR Code
    // ----------------------------------------------------

    public async Task<bool> RevokeAsync(
        Guid id,
        RevokeQRCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException(
                "A revocation reason is required.");
        }

        var qrCode = await _qrCodeRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (qrCode is null)
        {
            return false;
        }

        if (qrCode.IsRevoked)
        {
            return true;
        }

        qrCode.IsRevoked = true;

        await _qrCodeRepository.UpdateAsync(
            qrCode,
            cancellationToken);

        var reservation = await _reservationRepository.GetByIdAsync(
          qrCode.ReservationId,
          cancellationToken);

        if (reservation is not null)
        {
            await _notificationDispatcher.DispatchAsync(
                reservation.UserId,
                "QR Code Revoked",
                $"The QR code for reservation {reservation.ReservationNumber} has been revoked. Reason: {request.Reason.Trim()}",
                "QRCode",
                qrCode.Id.ToString(),
                cancellationToken: cancellationToken);
        }

        return true;
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        QRCodeQueryParameters parameters)
    {
        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (parameters.ExpiresFrom.HasValue &&
            parameters.ExpiresTo.HasValue &&
            parameters.ExpiresFrom.Value > parameters.ExpiresTo.Value)
        {
            throw new ArgumentException(
                "ExpiresFrom cannot be later than ExpiresTo.");
        }

        if (parameters.MinUsageCount.HasValue &&
            parameters.MinUsageCount.Value < 0)
        {
            throw new ArgumentException(
                "MinUsageCount cannot be negative.");
        }

        if (parameters.MaxUsageCount.HasValue &&
            parameters.MaxUsageCount.Value < 0)
        {
            throw new ArgumentException(
                "MaxUsageCount cannot be negative.");
        }

        if (parameters.MinUsageCount.HasValue &&
            parameters.MaxUsageCount.HasValue &&
            parameters.MinUsageCount.Value > parameters.MaxUsageCount.Value)
        {
            throw new ArgumentException(
                "MinUsageCount cannot be greater than MaxUsageCount.");
        }
    }

    // ----------------------------------------------------
    // Compute Token Hash
    // ----------------------------------------------------

    private static string ComputeTokenHash(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private QRCodeResponse MapToResponse(QRCode qrCode)
    {
        return _mapper.Map<QRCodeResponse>(qrCode);
    }

    private PagedResult<QRCodeResponse> MapToPagedResponse(
        PagedResult<QRCode> result)
    {
        return new PagedResult<QRCodeResponse>
        {
            Items = _mapper.Map<List<QRCodeResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
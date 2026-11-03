using Parky.Application.Common;
using Parky.Application.DTOs.QRCode;

namespace Parky.Application.Interfaces.Services;

public interface IQRCodeService
{
    Task<CreatedQRCodeResponse> CreateAsync(
        CreateQRCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<QRCodeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QRCodeResponse>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<QRCodeResponse>> GetPagedByReservationIdAsync(
        Guid reservationId,
        QRCodeQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<QRCodeResponse?> ValidateAndUseAsync(
        ValidateQRCodeRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid id,
        RevokeQRCodeRequest request,
        CancellationToken cancellationToken = default);
}
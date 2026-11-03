using Parky.Application.Common;
using Parky.Application.DTOs.QRCode;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IQRCodeRepository
{
    Task<QRCode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<QRCode?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QRCode>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<QRCode>> GetPagedByReservationIdAsync(
        Guid reservationId,
        QRCodeQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveCodeForReservationAsync(
        Guid reservationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default);
}
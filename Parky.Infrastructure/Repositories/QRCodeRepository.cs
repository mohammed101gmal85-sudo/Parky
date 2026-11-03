using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.QRCode;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class QRCodeRepository : IQRCodeRepository
{
    private readonly ParkyDbContext _dbContext;

    public QRCodeRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QRCode?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<QRCode>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                qrCode => qrCode.Id == id,
                cancellationToken);
    }

    public async Task<QRCode?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<QRCode>()
            .FirstOrDefaultAsync(
                qrCode => qrCode.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<QRCode>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<QRCode>()
            .AsNoTracking()
            .Where(qrCode => qrCode.ReservationId == reservationId)
            .OrderByDescending(qrCode => qrCode.CreatedAt)
            .ThenByDescending(qrCode => qrCode.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<QRCode>> GetPagedByReservationIdAsync(
        Guid reservationId,
        QRCodeQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<QRCode>()
            .AsNoTracking()
            .Where(qrCode => qrCode.ReservationId == reservationId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<bool> HasActiveCodeForReservationAsync(
        Guid reservationId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<QRCode>()
            .AsNoTracking()
            .AnyAsync(
                qrCode =>
                    qrCode.ReservationId == reservationId &&
                    !qrCode.IsRevoked &&
                    qrCode.ExpiresAt > now &&
                    qrCode.UsedAt == null,
                cancellationToken);
    }

    public async Task AddAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<QRCode>()
            .AddAsync(qrCode, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<QRCode>().Update(qrCode);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        QRCode qrCode,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<QRCode>().Remove(qrCode);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<QRCode> ApplyFilters(
        IQueryable<QRCode> query,
        QRCodeQueryParameters parameters)
    {
        if (parameters.IsRevoked.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.IsRevoked == parameters.IsRevoked.Value);
        }

        if (parameters.IsUsed.HasValue)
        {
            if (parameters.IsUsed.Value)
            {
                query = query.Where(qrCode =>
                    qrCode.UsedAt.HasValue);
            }
            else
            {
                query = query.Where(qrCode =>
                    !qrCode.UsedAt.HasValue);
            }
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.ExpiresFrom.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.ExpiresAt >= parameters.ExpiresFrom.Value);
        }

        if (parameters.ExpiresTo.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.ExpiresAt <= parameters.ExpiresTo.Value);
        }

        if (parameters.MinUsageCount.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.UsageCount >= parameters.MinUsageCount.Value);
        }

        if (parameters.MaxUsageCount.HasValue)
        {
            query = query.Where(qrCode =>
                qrCode.UsageCount <= parameters.MaxUsageCount.Value);
        }

        return query;
    }

    private static async Task<PagedResult<QRCode>> CreatePagedResultAsync(
        IQueryable<QRCode> query,
        QRCodeQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(qrCode => qrCode.CreatedAt)
            .ThenByDescending(qrCode => qrCode.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<QRCode>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
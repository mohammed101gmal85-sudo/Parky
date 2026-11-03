using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Payment;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ParkyDbContext _dbContext;

    public PaymentRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Payment>> GetPagedAsync(
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Payment>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }


    public async Task<PagedResult<Payment>> GetPagedByReservationIdAsync(
        Guid reservationId,
        PaymentQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Payment>()
            .AsNoTracking()
            .Where(payment => payment.ReservationId == reservationId);

        query = ApplyFilters(query, parameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(payment => payment.CreatedAt)
            .ThenByDescending(payment => payment.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payment>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }


    public async Task<IReadOnlyList<Payment>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .Where(x => x.ReservationId == reservationId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByPaymentNumberAsync(
        string paymentNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = paymentNumber.Trim();

        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.PaymentNumber == normalizedNumber,
                cancellationToken);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = idempotencyKey.Trim();

        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IdempotencyKey == normalizedKey,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>()
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> PaymentNumberExistsAsync(
        string paymentNumber,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = paymentNumber.Trim();

        return await _dbContext.Set<Payment>()
            .AnyAsync(
                x => x.PaymentNumber == normalizedNumber
                     && (!excludedId.HasValue || x.Id != excludedId.Value),
                cancellationToken);
    }

    public async Task<bool> IdempotencyKeyExistsAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = idempotencyKey.Trim();

        return await _dbContext.Set<Payment>()
            .AnyAsync(
                x => x.IdempotencyKey == normalizedKey,
                cancellationToken);
    }

    public async Task AddAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Payment>()
            .AddAsync(payment, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Payment>().Update(payment);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Payment>().Remove(payment);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Payment> ApplyFilters(
        IQueryable<Payment> query,
        PaymentQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(x =>
                x.PaymentNumber.ToLower().Contains(searchTerm)
                || (x.PaymentProvider != null
                    && x.PaymentProvider.ToLower().Contains(searchTerm))
                || (x.ProviderTransactionId != null
                    && x.ProviderTransactionId.ToLower().Contains(searchTerm)));
        }

        if (parameters.ReservationId.HasValue
            && parameters.ReservationId.Value != Guid.Empty)
        {
            query = query.Where(
                x => x.ReservationId == parameters.ReservationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PaymentMethod))
        {
            var paymentMethod = parameters.PaymentMethod.Trim().ToLower();

            query = query.Where(
                x => x.PaymentMethod.ToLower() == paymentMethod);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PaymentStatus))
        {
            var paymentStatus = parameters.PaymentStatus.Trim().ToLower();

            query = query.Where(
                x => x.PaymentStatus.ToLower() == paymentStatus);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Currency))
        {
            var currency = parameters.Currency.Trim().ToUpper();

            query = query.Where(
                x => x.Currency == currency);
        }

        if (parameters.MinAmount.HasValue)
        {
            query = query.Where(
                x => x.Amount >= parameters.MinAmount.Value);
        }

        if (parameters.MaxAmount.HasValue)
        {
            query = query.Where(
                x => x.Amount <= parameters.MaxAmount.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(
                x => x.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(
                x => x.CreatedAt <= parameters.CreatedTo.Value);
        }

        return query;
    }
}
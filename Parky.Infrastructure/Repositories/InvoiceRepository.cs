using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Invoice;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ParkyDbContext _dbContext;

    public InvoiceRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Invoice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Invoice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Invoice>()
            .AsNoTracking()
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Invoice>> GetPagedAsync(
        InvoiceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Invoice>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.IssuedAt)
            .ThenByDescending(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<Invoice>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Invoice>()
            .AsNoTracking()
            .Where(x => x.ReservationId == reservationId)
            .OrderByDescending(x => x.IssuedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = invoiceNumber.Trim();

        return await _dbContext.Set<Invoice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.InvoiceNumber == normalizedNumber,
                cancellationToken);
    }

    public async Task<bool> InvoiceNumberExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken = default)
    {
        var normalizedNumber = invoiceNumber.Trim();

        return await _dbContext.Set<Invoice>()
            .AnyAsync(
                x => x.InvoiceNumber == normalizedNumber,
                cancellationToken);
    }

    public async Task<bool> ExistsForReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Invoice>()
            .AnyAsync(
                x => x.ReservationId == reservationId,
                cancellationToken);
    }

    public async Task AddAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Invoice>()
            .AddAsync(invoice, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Invoice>().Update(invoice);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Invoice>().Remove(invoice);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Invoice> ApplyFilters(
        IQueryable<Invoice> query,
        InvoiceQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(x =>
                x.InvoiceNumber.ToLower().Contains(searchTerm)
                || (x.Notes != null
                    && x.Notes.ToLower().Contains(searchTerm)));
        }

        if (parameters.ReservationId.HasValue
            && parameters.ReservationId.Value != Guid.Empty)
        {
            query = query.Where(
                x => x.ReservationId == parameters.ReservationId.Value);
        }

        if (parameters.PaymentId.HasValue
            && parameters.PaymentId.Value != Guid.Empty)
        {
            query = query.Where(
                x => x.PaymentId == parameters.PaymentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.InvoiceStatus))
        {
            var status = parameters.InvoiceStatus.Trim().ToLower();

            query = query.Where(
                x => x.InvoiceStatus.ToLower() == status);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Currency))
        {
            var currency = parameters.Currency.Trim().ToUpper();

            query = query.Where(
                x => x.Currency == currency);
        }

        if (parameters.MinTotalAmount.HasValue)
        {
            query = query.Where(
                x => x.TotalAmount >= parameters.MinTotalAmount.Value);
        }

        if (parameters.MaxTotalAmount.HasValue)
        {
            query = query.Where(
                x => x.TotalAmount <= parameters.MaxTotalAmount.Value);
        }

        if (parameters.IssuedFrom.HasValue)
        {
            query = query.Where(
                x => x.IssuedAt >= parameters.IssuedFrom.Value);
        }

        if (parameters.IssuedTo.HasValue)
        {
            query = query.Where(
                x => x.IssuedAt <= parameters.IssuedTo.Value);
        }

        if (parameters.DueFrom.HasValue)
        {
            query = query.Where(
                x => x.DueAt.HasValue
                     && x.DueAt.Value >= parameters.DueFrom.Value);
        }

        if (parameters.DueTo.HasValue)
        {
            query = query.Where(
                x => x.DueAt.HasValue
                     && x.DueAt.Value <= parameters.DueTo.Value);
        }

        return query;
    }
}
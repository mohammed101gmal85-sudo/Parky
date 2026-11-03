
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.PricingRule;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class PricingRuleRepository : IPricingRuleRepository
{
    private readonly ParkyDbContext _dbContext;

    public PricingRuleRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PricingRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PricingRule>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                rule => rule.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PricingRule>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PricingRule>()
            .AsNoTracking()
            .OrderBy(rule => rule.ParkingId)
            .ThenByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<PricingRule>> GetPagedAsync(
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<PricingRule>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(
            query,
            parameters,
            includeParkingId: true);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PricingRule>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PricingRule>()
            .AsNoTracking()
            .Where(rule => rule.ParkingId == parkingId)
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ThenBy(rule => rule.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<PricingRule>> GetPagedByParkingIdAsync(
        Guid parkingId,
        PricingRuleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<PricingRule>()
            .AsNoTracking()
            .Where(rule => rule.ParkingId == parkingId);

        query = ApplyFilters(
            query,
            parameters,
            includeParkingId: false);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<PricingRule>()
            .AnyAsync(
                rule => rule.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<PricingRule>()
            .AddAsync(pricingRule, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<PricingRule>().Update(pricingRule);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        PricingRule pricingRule,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<PricingRule>().Remove(pricingRule);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<PricingRule> ApplyFilters(
        IQueryable<PricingRule> query,
        PricingRuleQueryParameters parameters,
        bool includeParkingId)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm
                .Trim()
                .ToLower();

            query = query.Where(rule =>
                rule.Name.ToLower().Contains(searchTerm) ||
                rule.PricingType.ToLower().Contains(searchTerm));
        }

        if (includeParkingId && parameters.ParkingId.HasValue)
        {
            query = query.Where(rule =>
                rule.ParkingId == parameters.ParkingId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.PricingType))
        {
            var pricingType = parameters.PricingType
                .Trim()
                .ToLower();

            query = query.Where(rule =>
                rule.PricingType.ToLower() == pricingType);
        }

        if (parameters.DayOfWeek.HasValue)
        {
            query = query.Where(rule =>
                rule.DayOfWeek == parameters.DayOfWeek.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(rule =>
                rule.IsActive == parameters.IsActive.Value);
        }

        if (parameters.MinPriority.HasValue)
        {
            query = query.Where(rule =>
                rule.Priority >= parameters.MinPriority.Value);
        }

        if (parameters.MaxPriority.HasValue)
        {
            query = query.Where(rule =>
                rule.Priority <= parameters.MaxPriority.Value);
        }

        if (parameters.MinPricePerHour.HasValue)
        {
            query = query.Where(rule =>
                rule.PricePerHour >= parameters.MinPricePerHour.Value);
        }

        if (parameters.MaxPricePerHour.HasValue)
        {
            query = query.Where(rule =>
                rule.PricePerHour <= parameters.MaxPricePerHour.Value);
        }

        if (parameters.MinFlatPrice.HasValue)
        {
            query = query.Where(rule =>
                rule.FlatPrice.HasValue &&
                rule.FlatPrice.Value >= parameters.MinFlatPrice.Value);
        }

        if (parameters.MaxFlatPrice.HasValue)
        {
            query = query.Where(rule =>
                rule.FlatPrice.HasValue &&
                rule.FlatPrice.Value <= parameters.MaxFlatPrice.Value);
        }

        if (parameters.MinMaximumDailyPrice.HasValue)
        {
            query = query.Where(rule =>
                rule.MaximumDailyPrice.HasValue &&
                rule.MaximumDailyPrice.Value >=
                parameters.MinMaximumDailyPrice.Value);
        }

        if (parameters.MaxMaximumDailyPrice.HasValue)
        {
            query = query.Where(rule =>
                rule.MaximumDailyPrice.HasValue &&
                rule.MaximumDailyPrice.Value <=
                parameters.MaxMaximumDailyPrice.Value);
        }

        if (parameters.EffectiveAt.HasValue)
        {
            var effectiveAt = parameters.EffectiveAt.Value;

            query = query.Where(rule =>
                (!rule.EffectiveFrom.HasValue ||
                 rule.EffectiveFrom.Value <= effectiveAt) &&
                (!rule.EffectiveTo.HasValue ||
                 rule.EffectiveTo.Value >= effectiveAt));
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(rule =>
                rule.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(rule =>
                rule.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(rule =>
                rule.UpdatedAt.HasValue &&
                rule.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(rule =>
                rule.UpdatedAt.HasValue &&
                rule.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<PricingRule>>
        CreatePagedResultAsync(
            IQueryable<PricingRule> query,
            PricingRuleQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(rule => rule.ParkingId)
            .ThenByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name)
            .ThenBy(rule => rule.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PricingRule>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

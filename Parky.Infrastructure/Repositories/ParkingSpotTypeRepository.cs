
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpotType;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingSpotTypeRepository : IParkingSpotTypeRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingSpotTypeRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingSpotType?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpotType>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                type => type.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpotType>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpotType>()
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ThenBy(type => type.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingSpotType>> GetPagedAsync(
        ParkingSpotTypeQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingSpotType>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpotType>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpotType>()
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.Name)
            .ThenBy(type => type.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpotType>()
            .AnyAsync(
                type => type.Id == id,
                cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return await _dbContext.Set<ParkingSpotType>()
            .AnyAsync(
                type =>
                    type.Name == normalizedName &&
                    (!excludedId.HasValue ||
                     type.Id != excludedId.Value),
                cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(
        string code,
        Guid? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return await _dbContext.Set<ParkingSpotType>()
            .AnyAsync(
                type =>
                    type.Code.ToUpper() == normalizedCode &&
                    (!excludedId.HasValue ||
                     type.Id != excludedId.Value),
                cancellationToken);
    }

    public async Task<bool> HasAssignedParkingSpotsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AnyAsync(
                spot => spot.ParkingSpotTypeId == id,
                cancellationToken);
    }

    public async Task AddAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingSpotType>()
            .AddAsync(parkingSpotType, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingSpotType>().Update(parkingSpotType);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        ParkingSpotType parkingSpotType,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingSpotType>().Remove(parkingSpotType);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingSpotType> ApplyFilters(
        IQueryable<ParkingSpotType> query,
        ParkingSpotTypeQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm
                .Trim()
                .ToLower();

            query = query.Where(type =>
                type.Name.ToLower().Contains(searchTerm) ||
                type.Code.ToLower().Contains(searchTerm) ||
                (type.Description != null &&
                 type.Description.ToLower().Contains(searchTerm)));
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(type =>
                type.IsActive == parameters.IsActive.Value);
        }

        if (parameters.MinAdditionalHourlyRate.HasValue)
        {
            query = query.Where(type =>
                type.AdditionalHourlyRate.HasValue &&
                type.AdditionalHourlyRate.Value >=
                parameters.MinAdditionalHourlyRate.Value);
        }

        if (parameters.MaxAdditionalHourlyRate.HasValue)
        {
            query = query.Where(type =>
                type.AdditionalHourlyRate.HasValue &&
                type.AdditionalHourlyRate.Value <=
                parameters.MaxAdditionalHourlyRate.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(type =>
                type.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(type =>
                type.CreatedAt <= parameters.CreatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingSpotType>>
        CreatePagedResultAsync(
            IQueryable<ParkingSpotType> query,
            ParkingSpotTypeQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(type => type.Name)
            .ThenBy(type => type.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingSpotType>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

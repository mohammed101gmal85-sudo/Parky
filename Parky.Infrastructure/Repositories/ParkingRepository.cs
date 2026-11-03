
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Parking;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingRepository : IParkingRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Parking?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Parkings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                parking => parking.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Parking>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Parkings
            .AsNoTracking()
            .OrderBy(parking => parking.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Parking>> GetByOwnerIdAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Parkings
            .AsNoTracking()
            .Where(parking => parking.OwnerId == ownerId)
            .OrderBy(parking => parking.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Parking>> GetPagedAsync(
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Parkings
            .AsNoTracking()
            .Where(parking => parking.IsActive);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<PagedResult<Parking>> GetPagedByOwnerIdAsync(
        Guid ownerId,
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Parkings
            .AsNoTracking()
            .Where(parking => parking.OwnerId == ownerId);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task AddAsync(
        Parking parking,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Parkings.AddAsync(
            parking,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Parking parking,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Parkings.Update(parking);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Parkings
            .AnyAsync(
                parking => parking.Id == id,
                cancellationToken);
    }

    private static IQueryable<Parking> ApplyFilters(
        IQueryable<Parking> query,
        ParkingQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(parking =>
                parking.Name.ToLower().Contains(searchTerm) ||
                parking.Description.ToLower().Contains(searchTerm) ||
                parking.Address.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Governorate))
        {
            var governorate = parameters.Governorate.Trim().ToLower();

            query = query.Where(parking =>
                parking.Governorate.ToLower() == governorate);
        }

        if (!string.IsNullOrWhiteSpace(parameters.City))
        {
            var city = parameters.City.Trim().ToLower();

            query = query.Where(parking =>
                parking.City.ToLower() == city);
        }

        return query;
    }

    private static async Task<PagedResult<Parking>> BuildPagedResultAsync(
        IQueryable<Parking> query,
        PaginationRequest parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(parking => parking.Name)
            .ThenBy(parking => parking.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Parking>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

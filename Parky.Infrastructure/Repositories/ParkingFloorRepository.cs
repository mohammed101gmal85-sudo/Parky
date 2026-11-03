using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingFloorRepository : IParkingFloorRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingFloorRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingFloor?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                floor => floor.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingFloor>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AsNoTracking()
            .OrderBy(floor => floor.ParkingId)
            .ThenBy(floor => floor.FloorNumber)
            .ThenBy(floor => floor.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingFloor>> GetPagedAsync(
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingFloor>()
            .AsNoTracking()
            .Where(floor => floor.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingFloor>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AsNoTracking()
            .Where(floor =>
                floor.ParkingId == parkingId &&
                floor.IsActive)
            .OrderBy(floor => floor.FloorNumber)
            .ThenBy(floor => floor.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingFloor>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingFloor>()
            .AsNoTracking()
            .Where(floor =>
                floor.ParkingId == parkingId &&
                floor.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AnyAsync(
                floor => floor.Id == id,
                cancellationToken);
    }

    public async Task<bool> ParkingExistsAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Parking>()
            .AnyAsync(
                parking =>
                    parking.Id == parkingId &&
                    parking.IsActive,
                cancellationToken);
    }

    public async Task<bool> ParkingBelongsToOwnerAsync(
        Guid parkingId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Parking>()
            .AnyAsync(
                parking =>
                    parking.Id == parkingId &&
                    parking.OwnerId == ownerId &&
                    parking.IsActive,
                cancellationToken);
    }

    public async Task<bool> FloorNumberExistsAsync(
        Guid parkingId,
        int floorNumber,
        Guid? excludedFloorId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingFloor>()
            .Where(floor =>
                floor.ParkingId == parkingId &&
                floor.FloorNumber == floorNumber);

        if (excludedFloorId.HasValue)
        {
            query = query.Where(
                floor => floor.Id != excludedFloorId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        ParkingFloor floor,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingFloor>()
            .AddAsync(floor, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingFloor floor,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingFloor>().Update(floor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingFloor> ApplyFilters(
        IQueryable<ParkingFloor> query,
        ParkingFloorQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(floor =>
                floor.Name.ToLower().Contains(searchTerm));
        }

        if (parameters.ParkingId.HasValue)
        {
            query = query.Where(floor =>
                floor.ParkingId == parameters.ParkingId.Value);
        }

        if (parameters.FloorNumber.HasValue)
        {
            query = query.Where(floor =>
                floor.FloorNumber == parameters.FloorNumber.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(floor =>
                floor.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(floor =>
                floor.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(floor =>
                floor.UpdatedAt.HasValue &&
                floor.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(floor =>
                floor.UpdatedAt.HasValue &&
                floor.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingFloor>> CreatePagedResultAsync(
        IQueryable<ParkingFloor> query,
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(floor => floor.ParkingId)
            .ThenBy(floor => floor.FloorNumber)
            .ThenBy(floor => floor.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingFloor>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
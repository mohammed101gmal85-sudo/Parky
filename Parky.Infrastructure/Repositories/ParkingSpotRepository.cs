
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingSpotRepository : IParkingSpotRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingSpotRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingSpot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpot>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .OrderBy(x => x.ParkingFloorId)
            .ThenBy(x => x.SpotNumber)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpot>> GetByParkingFloorIdAsync(
        Guid parkingFloorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .Where(x => x.ParkingFloorId == parkingFloorId)
            .OrderBy(x => x.SpotNumber)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingSpot>>
        GetAvailableByParkingFloorIdAsync(
            Guid parkingFloorId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .Where(x =>
                x.ParkingFloorId == parkingFloorId &&
                x.IsActive &&
                x.IsAvailable)
            .OrderBy(x => x.SpotNumber)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingSpot>> GetPagedAsync(
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<PagedResult<ParkingSpot>> GetPagedByParkingFloorIdAsync(
        Guid parkingFloorId,
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .Where(x => x.ParkingFloorId == parkingFloorId);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<PagedResult<ParkingSpot>>
        GetPagedAvailableByParkingFloorIdAsync(
            Guid parkingFloorId,
            ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingSpot>()
            .AsNoTracking()
            .Where(x =>
                x.ParkingFloorId == parkingFloorId &&
                x.IsActive &&
                x.IsAvailable);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpot>()
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<bool> ParkingFloorExistsAsync(
        Guid parkingFloorId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AnyAsync(
                x => x.Id == parkingFloorId,
                cancellationToken);
    }

    public async Task<bool> ParkingFloorBelongsToOwnerAsync(
        Guid parkingFloorId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingFloor>()
            .AnyAsync(
                x => x.Id == parkingFloorId &&
                     x.Parking.OwnerId == ownerId,
                cancellationToken);
    }

    public async Task<bool> ParkingSpotTypeExistsAsync(
        Guid parkingSpotTypeId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingSpotType>()
            .AnyAsync(
                x => x.Id == parkingSpotTypeId,
                cancellationToken);
    }

    public async Task<bool> SpotNumberExistsAsync(
        Guid parkingFloorId,
        string spotNumber,
        Guid? excludedSpotId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSpotNumber = spotNumber.Trim();

        return await _dbContext.Set<ParkingSpot>()
            .AnyAsync(
                x => x.ParkingFloorId == parkingFloorId &&
                     x.SpotNumber == normalizedSpotNumber &&
                     (!excludedSpotId.HasValue ||
                      x.Id != excludedSpotId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingSpot>()
            .AddAsync(parkingSpot, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingSpot>().Update(parkingSpot);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        ParkingSpot parkingSpot,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingSpot>().Remove(parkingSpot);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingSpot> ApplyFilters(
        IQueryable<ParkingSpot> query,
        ParkingSpotQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(x =>
                x.SpotNumber.ToLower().Contains(searchTerm) ||
                (x.Description != null &&
                 x.Description.ToLower().Contains(searchTerm)));
        }

        if (parameters.ParkingFloorId.HasValue &&
            parameters.ParkingFloorId.Value != Guid.Empty)
        {
            var parkingFloorId = parameters.ParkingFloorId.Value;

            query = query.Where(x =>
                x.ParkingFloorId == parkingFloorId);
        }

        if (parameters.ParkingSpotTypeId.HasValue &&
            parameters.ParkingSpotTypeId.Value != Guid.Empty)
        {
            var parkingSpotTypeId = parameters.ParkingSpotTypeId.Value;

            query = query.Where(x =>
                x.ParkingSpotTypeId == parkingSpotTypeId);
        }

        if (parameters.IsAvailable.HasValue)
        {
            var isAvailable = parameters.IsAvailable.Value;

            query = query.Where(x =>
                x.IsAvailable == isAvailable);
        }

        if (parameters.IsActive.HasValue)
        {
            var isActive = parameters.IsActive.Value;

            query = query.Where(x =>
                x.IsActive == isActive);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingSpot>>
        BuildPagedResultAsync(
            IQueryable<ParkingSpot> query,
            PaginationRequest parameters,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.ParkingFloorId)
            .ThenBy(x => x.SpotNumber)
            .ThenBy(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingSpot>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

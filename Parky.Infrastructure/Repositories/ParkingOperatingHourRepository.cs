
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingOperatingHour;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingOperatingHourRepository
    : IParkingOperatingHourRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingOperatingHourRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingOperatingHour?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                hour => hour.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingOperatingHour>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AsNoTracking()
            .OrderBy(hour => hour.ParkingId)
            .ThenBy(hour => hour.DayOfWeek)
            .ThenBy(hour => hour.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingOperatingHour>> GetPagedAsync(
        ParkingOperatingHourQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingOperatingHour>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingOperatingHour>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AsNoTracking()
            .Where(hour => hour.ParkingId == parkingId)
            .OrderBy(hour => hour.DayOfWeek)
            .ThenBy(hour => hour.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ParkingOperatingHour?> GetByParkingAndDayAsync(
        Guid parkingId,
        DayOfWeek dayOfWeek,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                hour =>
                    hour.ParkingId == parkingId &&
                    hour.DayOfWeek == dayOfWeek,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AnyAsync(
                hour => hour.Id == id,
                cancellationToken);
    }

    public async Task<bool> ParkingExistsAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Parking>()
            .AnyAsync(
                parking => parking.Id == parkingId,
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
                    parking.OwnerId == ownerId,
                cancellationToken);
    }

    public async Task<bool> DayExistsAsync(
        Guid parkingId,
        DayOfWeek dayOfWeek,
        Guid? excludedOperatingHourId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingOperatingHour>()
            .AnyAsync(
                hour =>
                    hour.ParkingId == parkingId &&
                    hour.DayOfWeek == dayOfWeek &&
                    (!excludedOperatingHourId.HasValue ||
                     hour.Id != excludedOperatingHourId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingOperatingHour>()
            .AddAsync(operatingHour, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingOperatingHour>()
            .Update(operatingHour);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        ParkingOperatingHour operatingHour,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingOperatingHour>()
            .Remove(operatingHour);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingOperatingHour> ApplyFilters(
        IQueryable<ParkingOperatingHour> query,
        ParkingOperatingHourQueryParameters parameters)
    {
        if (parameters.ParkingId.HasValue)
        {
            query = query.Where(hour =>
                hour.ParkingId == parameters.ParkingId.Value);
        }

        if (parameters.DayOfWeek.HasValue)
        {
            query = query.Where(hour =>
                hour.DayOfWeek == parameters.DayOfWeek.Value);
        }

        if (parameters.IsClosed.HasValue)
        {
            query = query.Where(hour =>
                hour.IsClosed == parameters.IsClosed.Value);
        }

        if (parameters.IsOpen24Hours.HasValue)
        {
            query = query.Where(hour =>
                hour.IsOpen24Hours == parameters.IsOpen24Hours.Value);
        }

        if (parameters.OpenTimeFrom.HasValue)
        {
            query = query.Where(hour =>
                hour.OpenTime >= parameters.OpenTimeFrom.Value);
        }

        if (parameters.OpenTimeTo.HasValue)
        {
            query = query.Where(hour =>
                hour.OpenTime <= parameters.OpenTimeTo.Value);
        }

        if (parameters.CloseTimeFrom.HasValue)
        {
            query = query.Where(hour =>
                hour.CloseTime >= parameters.CloseTimeFrom.Value);
        }

        if (parameters.CloseTimeTo.HasValue)
        {
            query = query.Where(hour =>
                hour.CloseTime <= parameters.CloseTimeTo.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(hour =>
                hour.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(hour =>
                hour.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(hour =>
                hour.UpdatedAt.HasValue &&
                hour.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(hour =>
                hour.UpdatedAt.HasValue &&
                hour.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingOperatingHour>>
        CreatePagedResultAsync(
            IQueryable<ParkingOperatingHour> query,
            ParkingOperatingHourQueryParameters parameters,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(hour => hour.ParkingId)
            .ThenBy(hour => hour.DayOfWeek)
            .ThenBy(hour => hour.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingOperatingHour>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

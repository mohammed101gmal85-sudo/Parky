
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Reservation;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly ParkyDbContext _dbContext;

    public ReservationRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Reservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .AsNoTracking()
            .Where(x => x.ParkingId == parkingId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Reservation>> GetPagedAsync(
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Reservation>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<PagedResult<Reservation>> GetPagedByUserIdAsync(
        Guid userId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Reservation>()
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<PagedResult<Reservation>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ReservationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Reservation>()
            .AsNoTracking()
            .Where(x => x.ParkingId == parkingId);

        query = ApplyFilters(query, parameters);

        return await BuildPagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<bool> ExistsByReservationNumberAsync(
        string reservationNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .AsNoTracking()
            .AnyAsync(
                x => x.ReservationNumber == reservationNumber,
                cancellationToken);
    }

    public async Task AddAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Reservation>()
            .AddAsync(reservation, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Reservation>().Update(reservation);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Reservation reservation,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Reservation>().Remove(reservation);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingReservationAsync(
        Guid parkingId,
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Reservation>()
            .AsNoTracking()
            .AnyAsync(
                x => x.ParkingId == parkingId
                     && (!excludeReservationId.HasValue
                         || x.Id != excludeReservationId.Value)
                     && x.Status != "Cancelled"
                     && x.Status != "Completed"
                     && x.StartTime < endTime
                     && startTime < x.EndTime,
                cancellationToken);
    }

    private static IQueryable<Reservation> ApplyFilters(
        IQueryable<Reservation> query,
        ReservationQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim();

            query = query.Where(x =>
                x.ReservationNumber.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.Trim().ToLower();

            query = query.Where(x =>
                x.Status.ToLower() == status);
        }

        if (parameters.ParkingId.HasValue &&
            parameters.ParkingId.Value != Guid.Empty)
        {
            var parkingId = parameters.ParkingId.Value;

            query = query.Where(x =>
                x.ParkingId == parkingId);
        }

        if (parameters.FromDate.HasValue)
        {
            var fromDate = parameters.FromDate.Value;

            query = query.Where(x =>
                x.StartTime >= fromDate);
        }

        if (parameters.ToDate.HasValue)
        {
            var toDate = parameters.ToDate.Value;

            query = query.Where(x =>
                x.StartTime <= toDate);
        }

        return query;
    }

    private static async Task<PagedResult<Reservation>>
        BuildPagedResultAsync(
            IQueryable<Reservation> query,
            PaginationRequest parameters,
            CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Reservation>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

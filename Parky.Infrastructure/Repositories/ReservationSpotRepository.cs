using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ReservationSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class ReservationSpotRepository : IReservationSpotRepository
{
    private readonly ParkyDbContext _dbContext;

    public ReservationSpotRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReservationSpot?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                reservationSpot => reservationSpot.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationSpot>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .OrderByDescending(reservationSpot => reservationSpot.CreatedAt)
            .ThenByDescending(reservationSpot => reservationSpot.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ReservationSpot>> GetPagedAsync(
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            oldestFirst: false);
    }

    public async Task<IReadOnlyList<ReservationSpot>> GetByReservationIdAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .Where(reservationSpot =>
                reservationSpot.ReservationId == reservationId)
            .OrderBy(reservationSpot => reservationSpot.CreatedAt)
            .ThenBy(reservationSpot => reservationSpot.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ReservationSpot>> GetPagedByReservationIdAsync(
        Guid reservationId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .Where(reservationSpot =>
                reservationSpot.ReservationId == reservationId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            oldestFirst: true);
    }

    public async Task<IReadOnlyList<ReservationSpot>> GetByParkingSpotIdAsync(
        Guid parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .Where(reservationSpot =>
                reservationSpot.ParkingSpotId == parkingSpotId)
            .OrderByDescending(reservationSpot => reservationSpot.CreatedAt)
            .ThenByDescending(reservationSpot => reservationSpot.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ReservationSpot>> GetPagedByParkingSpotIdAsync(
        Guid parkingSpotId,
        ReservationSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ReservationSpot>()
            .AsNoTracking()
            .Where(reservationSpot =>
                reservationSpot.ParkingSpotId == parkingSpotId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            oldestFirst: false);
    }

    public async Task<bool> ExistsAsync(
        Guid reservationId,
        Guid parkingSpotId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ReservationSpot>()
            .AnyAsync(
                reservationSpot =>
                    reservationSpot.ReservationId == reservationId &&
                    reservationSpot.ParkingSpotId == parkingSpotId,
                cancellationToken);
    }

    public async Task AddAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ReservationSpot>()
            .AddAsync(reservationSpot, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ReservationSpot>().Update(reservationSpot);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        ReservationSpot reservationSpot,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ReservationSpot>().Remove(reservationSpot);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ReservationSpot> ApplyFilters(
        IQueryable<ReservationSpot> query,
        ReservationSpotQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(reservationSpot =>
                reservationSpot.ParkingSpot.SpotNumber
                    .ToLower()
                    .Contains(searchTerm) ||
                (reservationSpot.ParkingSpot.Description != null &&
                 reservationSpot.ParkingSpot.Description
                    .ToLower()
                    .Contains(searchTerm)));
        }

        if (parameters.ReservationId.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.ReservationId ==
                parameters.ReservationId.Value);
        }

        if (parameters.ParkingSpotId.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.ParkingSpotId ==
                parameters.ParkingSpotId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.ReservationStatus))
        {
            var reservationStatus =
                parameters.ReservationStatus.Trim().ToLower();

            query = query.Where(reservationSpot =>
                reservationSpot.Reservation.Status.ToLower() ==
                reservationStatus);
        }

        if (parameters.MinPricePerHour.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.PricePerHour >=
                parameters.MinPricePerHour.Value);
        }

        if (parameters.MaxPricePerHour.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.PricePerHour <=
                parameters.MaxPricePerHour.Value);
        }

        if (parameters.MinReservedAmount.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.ReservedAmount >=
                parameters.MinReservedAmount.Value);
        }

        if (parameters.MaxReservedAmount.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.ReservedAmount <=
                parameters.MaxReservedAmount.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.CreatedAt >=
                parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(reservationSpot =>
                reservationSpot.CreatedAt <=
                parameters.CreatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ReservationSpot>>
        CreatePagedResultAsync(
            IQueryable<ReservationSpot> query,
            ReservationSpotQueryParameters parameters,
            CancellationToken cancellationToken,
            bool oldestFirst)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = oldestFirst
            ? query
                .OrderBy(reservationSpot => reservationSpot.CreatedAt)
                .ThenBy(reservationSpot => reservationSpot.Id)
            : query
                .OrderByDescending(reservationSpot => reservationSpot.CreatedAt)
                .ThenByDescending(reservationSpot => reservationSpot.Id);

        var items = await orderedQuery
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationSpot>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
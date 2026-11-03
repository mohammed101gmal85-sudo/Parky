using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Favorite;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly ParkyDbContext _dbContext;

    public FavoriteRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Favorite?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                favorite => favorite.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Favorite>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Favorite>> GetPagedAsync(
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Favorite>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Favorite>> GetPagedByUserIdAsync(
        Guid userId,
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Favorite>()
            .AsNoTracking()
            .Where(favorite => favorite.UserId == userId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Favorite>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .Where(favorite => favorite.ParkingId == parkingId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Favorite?> GetByUserAndParkingAsync(
        Guid userId,
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                favorite =>
                    favorite.UserId == userId &&
                    favorite.ParkingId == parkingId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Favorite>()
            .AnyAsync(
                favorite =>
                    favorite.UserId == userId &&
                    favorite.ParkingId == parkingId,
                cancellationToken);
    }

    public async Task AddAsync(
        Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Favorite>()
            .AddAsync(favorite, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Favorite favorite,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Favorite>().Remove(favorite);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Favorite> ApplyFilters(
        IQueryable<Favorite> query,
        FavoriteQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(favorite =>
                favorite.Parking.Name.ToLower().Contains(searchTerm) ||
                (favorite.Parking.Description != null &&
                 favorite.Parking.Description.ToLower().Contains(searchTerm)) ||
                (favorite.Parking.Address != null &&
                 favorite.Parking.Address.ToLower().Contains(searchTerm)) ||
                (favorite.Parking.Governorate != null &&
                 favorite.Parking.Governorate.ToLower().Contains(searchTerm)) ||
                (favorite.Parking.City != null &&
                 favorite.Parking.City.ToLower().Contains(searchTerm)));
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(favorite =>
                favorite.UserId == parameters.UserId.Value);
        }

        if (parameters.ParkingId.HasValue)
        {
            query = query.Where(favorite =>
                favorite.ParkingId == parameters.ParkingId.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(favorite =>
                favorite.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(favorite =>
                favorite.CreatedAt <= parameters.CreatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<Favorite>> CreatePagedResultAsync(
        IQueryable<Favorite> query,
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ThenByDescending(favorite => favorite.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Favorite>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
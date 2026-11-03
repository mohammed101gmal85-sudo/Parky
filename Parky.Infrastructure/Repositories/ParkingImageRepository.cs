
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingImage;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingImageRepository : IParkingImageRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingImageRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingImage?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingImage>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                image => image.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingImage>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingImage>()
            .AsNoTracking()
            .OrderBy(image => image.ParkingId)
            .ThenByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreatedAt)
            .ThenBy(image => image.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingImage>> GetPagedAsync(
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingImage>()
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

    public async Task<IReadOnlyList<ParkingImage>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingImage>()
            .AsNoTracking()
            .Where(image => image.ParkingId == parkingId)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreatedAt)
            .ThenBy(image => image.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingImage>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingImage>()
            .AsNoTracking()
            .Where(image => image.ParkingId == parkingId);

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
        return await _dbContext.Set<ParkingImage>()
            .AnyAsync(
                image => image.Id == id,
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

    public async Task<bool> HasPrimaryImageAsync(
        Guid parkingId,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingImage>()
            .Where(image =>
                image.ParkingId == parkingId &&
                image.IsPrimary);

        if (excludedImageId.HasValue)
        {
            query = query.Where(
                image => image.Id != excludedImageId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> DisplayOrderExistsAsync(
        Guid parkingId,
        int displayOrder,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingImage>()
            .Where(image =>
                image.ParkingId == parkingId &&
                image.DisplayOrder == displayOrder);

        if (excludedImageId.HasValue)
        {
            query = query.Where(
                image => image.Id != excludedImageId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingImage>()
            .AddAsync(image, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingImage>().Update(image);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        ParkingImage image,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingImage>().Remove(image);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingImage> ApplyFilters(
        IQueryable<ParkingImage> query,
        ParkingImageQueryParameters parameters,
        bool includeParkingId)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm
                .Trim()
                .ToLower();

            query = query.Where(image =>
                image.ImageUrl.ToLower().Contains(searchTerm) ||
                (image.PublicId != null &&
                 image.PublicId.ToLower().Contains(searchTerm)) ||
                (image.AltText != null &&
                 image.AltText.ToLower().Contains(searchTerm)));
        }

        if (includeParkingId && parameters.ParkingId.HasValue)
        {
            query = query.Where(image =>
                image.ParkingId == parameters.ParkingId.Value);
        }

        if (parameters.IsPrimary.HasValue)
        {
            query = query.Where(image =>
                image.IsPrimary == parameters.IsPrimary.Value);
        }

        if (parameters.MinDisplayOrder.HasValue)
        {
            query = query.Where(image =>
                image.DisplayOrder >= parameters.MinDisplayOrder.Value);
        }

        if (parameters.MaxDisplayOrder.HasValue)
        {
            query = query.Where(image =>
                image.DisplayOrder <= parameters.MaxDisplayOrder.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(image =>
                image.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(image =>
                image.CreatedAt <= parameters.CreatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingImage>> CreatePagedResultAsync(
        IQueryable<ParkingImage> query,
        ParkingImageQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(image => image.ParkingId)
            .ThenByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ThenBy(image => image.CreatedAt)
            .ThenBy(image => image.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingImage>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

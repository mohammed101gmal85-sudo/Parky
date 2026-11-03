using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ParkyDbContext _dbContext;

    public NotificationRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                notification => notification.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Notification>> GetPagedAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Notification>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Notification>> GetPagedByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                (!notification.ExpiresAt.HasValue ||
                 notification.ExpiresAt.Value > now));

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                !notification.IsRead &&
                (!notification.ExpiresAt.HasValue ||
                 notification.ExpiresAt.Value > now))
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Notification>> GetPagedUnreadByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _dbContext.Set<Notification>()
            .AsNoTracking()
            .Where(notification =>
                notification.UserId == userId &&
                !notification.IsRead &&
                (!notification.ExpiresAt.HasValue ||
                 notification.ExpiresAt.Value > now));

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.Set<Notification>()
            .AsNoTracking()
            .CountAsync(
                notification =>
                    notification.UserId == userId &&
                    !notification.IsRead &&
                    (!notification.ExpiresAt.HasValue ||
                     notification.ExpiresAt.Value > now),
                cancellationToken);
    }

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Notification>()
            .AddAsync(notification, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Notification>().Update(notification);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Notification>().Remove(notification);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Notification> ApplyFilters(
        IQueryable<Notification> query,
        NotificationQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(notification =>
                notification.Title.ToLower().Contains(searchTerm) ||
                notification.Message.ToLower().Contains(searchTerm) ||
                (notification.ReferenceId != null &&
                 notification.ReferenceId.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.NotificationType))
        {
            var notificationType =
                parameters.NotificationType.Trim().ToLower();

            query = query.Where(notification =>
                notification.NotificationType.ToLower() == notificationType);
        }

        if (parameters.IsRead.HasValue)
        {
            query = query.Where(notification =>
                notification.IsRead == parameters.IsRead.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(notification =>
                notification.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(notification =>
                notification.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.SentFrom.HasValue)
        {
            query = query.Where(notification =>
                notification.SentAt.HasValue &&
                notification.SentAt.Value >= parameters.SentFrom.Value);
        }

        if (parameters.SentTo.HasValue)
        {
            query = query.Where(notification =>
                notification.SentAt.HasValue &&
                notification.SentAt.Value <= parameters.SentTo.Value);
        }

        if (parameters.ExpiresFrom.HasValue)
        {
            query = query.Where(notification =>
                notification.ExpiresAt.HasValue &&
                notification.ExpiresAt.Value >= parameters.ExpiresFrom.Value);
        }

        if (parameters.ExpiresTo.HasValue)
        {
            query = query.Where(notification =>
                notification.ExpiresAt.HasValue &&
                notification.ExpiresAt.Value <= parameters.ExpiresTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<Notification>> CreatePagedResultAsync(
        IQueryable<Notification> query,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Notification>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
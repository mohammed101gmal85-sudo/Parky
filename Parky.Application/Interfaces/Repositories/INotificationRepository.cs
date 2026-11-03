using Parky.Application.Common;
using Parky.Application.DTOs.Notification;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> GetPagedAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> GetPagedByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Notification>> GetPagedUnreadByUserIdAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
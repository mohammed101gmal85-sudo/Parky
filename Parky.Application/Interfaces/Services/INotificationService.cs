using Parky.Application.Common;
using Parky.Application.DTOs.Notification;

namespace Parky.Application.Interfaces.Services;

public interface INotificationService
{
    Task<NotificationResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationResponse>> GetAllAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationResponse>> GetMyUnreadNotificationsAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetMyUnreadNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> GetMyUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<NotificationResponse> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<NotificationResponse> UpdateAsync(
        Guid id,
        UpdateNotificationRequest request,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);


}
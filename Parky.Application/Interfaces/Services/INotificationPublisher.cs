using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.DTOs.Notification;


namespace Parky.Application.Interfaces.Services;

public interface INotificationPublisher
{
    Task PublishNotificationAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);

    Task PublishNotificationUpdatedAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default);

    Task PublishNotificationDeletedAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task PublishNotificationsReadAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task PublishUnreadCountUpdatedAsync(
        Guid userId,
        int unreadCount,
        CancellationToken cancellationToken = default);
}
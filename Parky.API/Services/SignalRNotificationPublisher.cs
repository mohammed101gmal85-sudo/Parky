using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Parky.API.Hubs;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Services;

namespace Parky.API.Services;

public sealed class SignalRNotificationPublisher
    : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationPublisher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public Task PublishNotificationAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default)
    {
        return SendToUserAsync(
            userId,
            NotificationHub.NotificationReceivedEvent,
            notification,
            cancellationToken);
    }

    public Task PublishNotificationUpdatedAsync(
        Guid userId,
        NotificationResponse notification,
        CancellationToken cancellationToken = default)
    {
        return SendToUserAsync(
            userId,
            NotificationHub.NotificationUpdatedEvent,
            notification,
            cancellationToken);
    }

    public Task PublishNotificationDeletedAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        return SendToUserAsync(
            userId,
            NotificationHub.NotificationDeletedEvent,
            new { NotificationId = notificationId },
            cancellationToken);
    }

    public Task PublishNotificationsReadAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return SendToUserAsync(
            userId,
            NotificationHub.NotificationsReadAllEvent,
            new { UserId = userId },
            cancellationToken);
    }

    public Task PublishUnreadCountUpdatedAsync(
        Guid userId,
        int unreadCount,
        CancellationToken cancellationToken = default)
    {
        return SendToUserAsync(
            userId,
            NotificationHub.UnreadCountUpdatedEvent,
            new { UnreadCount = unreadCount },
            cancellationToken);
    }

    private async Task SendToUserAsync<T>(
        Guid userId,
        string eventName,
        T payload,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        try
        {
            var groupName =
                NotificationHub.GetUserGroupName(userId);

            await _hubContext.Clients
                .Group(groupName)
                .SendAsync(
                    eventName,
                    payload,
                    cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish SignalR event {EventName} to user {UserId}.",
                eventName,
                userId);
        }
    }
}
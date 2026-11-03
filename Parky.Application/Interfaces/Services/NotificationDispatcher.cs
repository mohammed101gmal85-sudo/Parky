using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Services;

namespace Parky.Infrastructure.Services;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        INotificationService notificationService,
        ILogger<NotificationDispatcher> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task DispatchAsync(
        Guid userId,
        string title,
        string message,
        string notificationType,
        string? referenceId = null,
        string? actionUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning(
                "Notification was skipped because the user ID is empty.");

            return;
        }

        try
        {
            var request = new CreateNotificationRequest
            {
                UserId = userId,
                Title = title,
                Message = message,
                NotificationType = notificationType,
                ReferenceId = referenceId,
                ActionUrl = actionUrl
            };

            await _notificationService.CreateAsync(
                request,
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
                "Failed to create notification for user {UserId}. Notification type: {NotificationType}",
                userId,
                notificationType);
        }
    }
}
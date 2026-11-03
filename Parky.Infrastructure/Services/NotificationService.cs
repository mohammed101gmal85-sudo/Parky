using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IMapper _mapper;
    private readonly INotificationPublisher _notificationPublisher;

    public NotificationService(
        INotificationRepository notificationRepository,
        IMapper mapper,
        INotificationPublisher notificationPublisher)
    {
        _notificationRepository = notificationRepository;
        _mapper = mapper;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<NotificationResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (notification is null)
        {
            return null;
        }

        return MapToResponse(notification);
    }

    public async Task<PagedResult<NotificationResponse>> GetAllAsync(
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _notificationRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        NotificationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _notificationRepository.GetPagedByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<NotificationResponse>>
        GetMyUnreadNotificationsAsync(
            Guid userId,
            NotificationQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _notificationRepository.GetPagedUnreadByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var notifications = await _notificationRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var activeNotifications = notifications
            .Where(notification =>
                !notification.ExpiresAt.HasValue ||
                notification.ExpiresAt.Value > now)
            .ToList();

        return _mapper.Map<List<NotificationResponse>>(
            activeNotifications);
    }

    public async Task<IReadOnlyList<NotificationResponse>>
        GetMyUnreadNotificationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(
            userId,
            cancellationToken);

        return _mapper.Map<List<NotificationResponse>>(
            notifications);
    }

    public async Task<int> GetMyUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        return await _notificationRepository.GetUnreadCountAsync(
            userId,
            cancellationToken);
    }

    public async Task<NotificationResponse> CreateAsync(
        CreateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidUserId(request.UserId);

        var title = request.Title?.Trim();
        var message = request.Message?.Trim();
        var notificationType = request.NotificationType?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Notification title is required.");
        }

        if (title.Length > 200)
        {
            throw new ArgumentException(
                "Notification title cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.");
        }

        if (message.Length > 2000)
        {
            throw new ArgumentException(
                "Notification message cannot exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException(
                "Notification type is required.");
        }

        if (notificationType.Length > 50)
        {
            throw new ArgumentException(
                "Notification type cannot exceed 50 characters.");
        }

        if (request.ReferenceId?.Length > 100)
        {
            throw new ArgumentException(
                "ReferenceId cannot exceed 100 characters.");
        }

        if (request.ActionUrl?.Length > 500)
        {
            throw new ArgumentException(
                "ActionUrl cannot exceed 500 characters.");
        }

        var now = DateTimeOffset.UtcNow;

        if (request.ExpiresAt.HasValue &&
            request.ExpiresAt.Value <= now)
        {
            throw new ArgumentException(
                "Expiration time must be in the future.");
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ReferenceId = NormalizeOptionalValue(request.ReferenceId),
            ActionUrl = NormalizeOptionalValue(request.ActionUrl),
            IsRead = false,
            ReadAt = null,
            SentAt = now,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now
        };

        await _notificationRepository.AddAsync(
            notification,
            cancellationToken);

        var response = MapToResponse(notification);

        await _notificationPublisher.PublishNotificationAsync(
            notification.UserId,
            response,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(
            notification.UserId,
            cancellationToken);

        await _notificationPublisher.PublishUnreadCountUpdatedAsync(
            notification.UserId,
            unreadCount,
            cancellationToken);

        return response;
    }

    public async Task<NotificationResponse> UpdateAsync(
        Guid id,
        UpdateNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = await _notificationRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (notification is null)
        {
            throw new KeyNotFoundException(
                "Notification was not found.");
        }

        var title = request.Title?.Trim();
        var message = request.Message?.Trim();
        var notificationType = request.NotificationType?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Notification title is required.");
        }

        if (title.Length > 200)
        {
            throw new ArgumentException(
                "Notification title cannot exceed 200 characters.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.");
        }

        if (message.Length > 2000)
        {
            throw new ArgumentException(
                "Notification message cannot exceed 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(notificationType))
        {
            throw new ArgumentException(
                "Notification type is required.");
        }

        if (notificationType.Length > 50)
        {
            throw new ArgumentException(
                "Notification type cannot exceed 50 characters.");
        }

        if (request.ReferenceId?.Length > 100)
        {
            throw new ArgumentException(
                "ReferenceId cannot exceed 100 characters.");
        }

        if (request.ActionUrl?.Length > 500)
        {
            throw new ArgumentException(
                "ActionUrl cannot exceed 500 characters.");
        }

        if (request.ExpiresAt.HasValue &&
            request.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException(
                "Expiration time must be in the future.");
        }

        notification.Title = title;
        notification.Message = message;
        notification.NotificationType = notificationType;
        notification.ReferenceId = NormalizeOptionalValue(
            request.ReferenceId);
        notification.ActionUrl = NormalizeOptionalValue(
            request.ActionUrl);
        notification.ExpiresAt = request.ExpiresAt;

        await _notificationRepository.UpdateAsync(
            notification,
            cancellationToken);

        var response = MapToResponse(notification);

        await _notificationPublisher.PublishNotificationUpdatedAsync(
            notification.UserId,
            response,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(
            notification.UserId,
            cancellationToken);

        await _notificationPublisher.PublishUnreadCountUpdatedAsync(
            notification.UserId,
            unreadCount,
            cancellationToken);

        return response;
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            cancellationToken);

        if (notification is null)
        {
            throw new KeyNotFoundException(
                "Notification was not found.");
        }

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You cannot mark another user's notification as read.");
        }

        if (notification.ExpiresAt.HasValue &&
            notification.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            throw new InvalidOperationException(
                "An expired notification cannot be marked as read.");
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;

        await _notificationRepository.UpdateAsync(
            notification,
            cancellationToken);

        var response = MapToResponse(notification);

        await _notificationPublisher.PublishNotificationUpdatedAsync(
            notification.UserId,
            response,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(
            userId,
            cancellationToken);

        await _notificationPublisher.PublishUnreadCountUpdatedAsync(
            userId,
            unreadCount,
            cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var notifications = await _notificationRepository.GetUnreadByUserIdAsync(
            userId,
            cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = now;

            await _notificationRepository.UpdateAsync(
                notification,
                cancellationToken);
        }

        await _notificationPublisher.PublishNotificationsReadAllAsync(
            userId,
            cancellationToken);

        var unreadCount = await _notificationRepository.GetUnreadCountAsync(
            userId,
            cancellationToken);

        await _notificationPublisher.PublishUnreadCountUpdatedAsync(
            userId,
            unreadCount,
            cancellationToken);
    }

    public async Task DeleteAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var notification = await _notificationRepository.GetByIdAsync(
            notificationId,
            cancellationToken);

        if (notification is null)
        {
            throw new KeyNotFoundException(
                "Notification was not found.");
        }

        if (notification.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You cannot delete another user's notification.");
        }

        var wasUnread = !notification.IsRead;

        await _notificationRepository.DeleteAsync(
            notification,
            cancellationToken);

        await _notificationPublisher.PublishNotificationDeletedAsync(
            userId,
            notificationId,
            cancellationToken);

        if (wasUnread)
        {
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(
                userId,
                cancellationToken);

            await _notificationPublisher.PublishUnreadCountUpdatedAsync(
                userId,
                unreadCount,
                cancellationToken);
        }
    }

    private static void EnsureValidUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is invalid.");
        }
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void ValidateQueryParameters(
        NotificationQueryParameters parameters)
    {
        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (parameters.SentFrom.HasValue &&
            parameters.SentTo.HasValue &&
            parameters.SentFrom.Value > parameters.SentTo.Value)
        {
            throw new ArgumentException(
                "SentFrom cannot be later than SentTo.");
        }

        if (parameters.ExpiresFrom.HasValue &&
            parameters.ExpiresTo.HasValue &&
            parameters.ExpiresFrom.Value > parameters.ExpiresTo.Value)
        {
            throw new ArgumentException(
                "ExpiresFrom cannot be later than ExpiresTo.");
        }

        if (parameters.NotificationType?.Length > 50)
        {
            throw new ArgumentException(
                "NotificationType cannot exceed 50 characters.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }
    }

    private NotificationResponse MapToResponse(
        Notification notification)
    {
        return _mapper.Map<NotificationResponse>(notification);
    }

    private PagedResult<NotificationResponse> MapToPagedResponse(
        PagedResult<Notification> result)
    {
        return new PagedResult<NotificationResponse>
        {
            Items = _mapper.Map<List<NotificationResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
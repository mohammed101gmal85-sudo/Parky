using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.Notification;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();

    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(
            _notificationRepositoryMock.Object,
            _mapperMock.Object,
            _notificationPublisherMock.Object
        );

        _mapperMock
            .Setup(x => x.Map<NotificationResponse>(
                It.IsAny<Notification>()))
            .Returns(new NotificationResponse());

        _mapperMock
            .Setup(x => x.Map<List<NotificationResponse>>(
                It.IsAny<List<Notification>>()))
            .Returns((List<Notification> notifications) =>
                new List<NotificationResponse>(
                    notifications.ConvertAll(_ => new NotificationResponse())));
    }

    private static CreateNotificationRequest CreateValidRequest(
        Guid? userId = null)
    {
        return new CreateNotificationRequest
        {
            UserId = userId ?? Guid.NewGuid(),
            Title = "Reservation Confirmed",
            Message = "Your reservation has been confirmed.",
            NotificationType = "Reservation",
            ReferenceId = "RES-1001",
            ActionUrl = "/reservations/1001"
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateNotificationAndPublishEvents()
    {
        var userId = Guid.NewGuid();
        var request = CreateValidRequest(userId);

        Notification? savedNotification = null;

        _notificationRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>(
                (notification, _) => savedNotification = notification)
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        savedNotification.Should().NotBeNull();

        savedNotification!.UserId.Should().Be(userId);
        savedNotification.Title.Should().Be(request.Title);
        savedNotification.IsRead.Should().BeFalse();
        savedNotification.ReadAt.Should().BeNull();

        _notificationRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationAsync(
                userId,
                It.IsAny<NotificationResponse>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishUnreadCountUpdatedAsync(
                userId,
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest(Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*User ID is invalid*");

        _notificationRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyTitle_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();
        request.Title = " ";

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Notification title is required*");

        _notificationRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyMessage_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();
        request.Message = " ";

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Notification message is required*");
    }

    [Fact]
    public async Task CreateAsync_WithLongActionUrl_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();
        request.ActionUrl = new string('A', 501);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*ActionUrl cannot exceed 500 characters*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationDoesNotExist_ShouldReturnNull()
    {
        var id = Guid.NewGuid();

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidUser_ShouldMarkNotificationAsRead()
    {
        var userId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Reservation",
            Message = "Reservation notification.",
            NotificationType = "Reservation",
            IsRead = false
        };

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                notification.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _notificationRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.MarkAsReadAsync(notification.Id, userId);

        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();

        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationUpdatedAsync(
                userId,
                It.IsAny<NotificationResponse>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishUnreadCountUpdatedAsync(
                userId,
                0,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToAnotherUser_ShouldThrowUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            IsRead = false
        };

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                notification.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var act = async () => await _sut.MarkAsReadAsync(
            notification.Id,
            anotherUserId);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>();

        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationIsAlreadyRead_ShouldNotUpdateAgain()
    {
        var userId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsRead = true,
            ReadAt = DateTimeOffset.UtcNow
        };

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                notification.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await _sut.MarkAsReadAsync(notification.Id, userId);

        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationUpdatedAsync(
                It.IsAny<Guid>(),
                It.IsAny<NotificationResponse>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithUnreadNotifications_ShouldMarkAllAndPublishEvents()
    {
        var userId = Guid.NewGuid();

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsRead = false
            }
        };

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        _notificationRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.MarkAllAsReadAsync(userId);

        notifications.Should().OnlyContain(x => x.IsRead);
        notifications.Should().OnlyContain(x => x.ReadAt.HasValue);

        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationsReadAllAsync(
                userId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishUnreadCountUpdatedAsync(
                userId,
                0,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WithNoUnreadNotifications_ShouldNotPublishEvents()
    {
        var userId = Guid.NewGuid();

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadByUserIdAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Notification>());

        await _sut.MarkAllAsReadAsync(userId);

        _notificationRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationsReadAllAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithUnreadNotification_ShouldDeleteAndUpdateUnreadCount()
    {
        var userId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsRead = false
        };

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                notification.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _notificationRepositoryMock
            .Setup(x => x.DeleteAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _sut.DeleteAsync(notification.Id, userId);

        _notificationRepositoryMock.Verify(
            x => x.DeleteAsync(
                notification,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishNotificationDeletedAsync(
                userId,
                notification.Id,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _notificationPublisherMock.Verify(
            x => x.PublishUnreadCountUpdatedAsync(
                userId,
                0,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationBelongsToAnotherUser_ShouldThrowUnauthorizedAccessException()
    {
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            IsRead = false
        };

        _notificationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                notification.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var act = async () => await _sut.DeleteAsync(
            notification.Id,
            anotherUserId);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>();

        _notificationRepositoryMock.Verify(
            x => x.DeleteAsync(
                It.IsAny<Notification>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

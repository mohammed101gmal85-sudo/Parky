
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.DeviceEvent;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class DeviceEventServiceTests
{
    private readonly Mock<IDeviceEventRepository> _deviceEventRepositoryMock = new();
    private readonly Mock<IParkingDeviceRepository> _parkingDeviceRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock = new();

    private readonly DeviceEventService _sut;

    public DeviceEventServiceTests()
    {
        _sut = new DeviceEventService(
            _deviceEventRepositoryMock.Object,
            _parkingDeviceRepositoryMock.Object,
            _mapperMock.Object,
            _notificationDispatcherMock.Object
        );

        _mapperMock
            .Setup(x => x.Map<DeviceEventResponse>(
                It.IsAny<DeviceEvent>()))
            .Returns(new DeviceEventResponse());
    }

    private static CreateDeviceEventRequest CreateValidRequest(
        Guid? deviceId = null)
    {
        return new CreateDeviceEventRequest
        {
            ParkingDeviceId = deviceId ?? Guid.NewGuid(),
            EventType = "EntryDetected",
            EventStatus = "Success",
            Description = "Vehicle detected at entry gate.",
            Payload = "{\"gate\":\"ENTRY-01\"}",
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateDeviceEvent()
    {
        var deviceId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _parkingDeviceRepositoryMock
            .Setup(x => x.GetParkingOwnerIdByDeviceIdAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownerId);

        DeviceEvent? savedEvent = null;

        _deviceEventRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<DeviceEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<DeviceEvent, CancellationToken>(
                (deviceEvent, _) => savedEvent = deviceEvent)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        savedEvent.Should().NotBeNull();

        savedEvent!.ParkingDeviceId.Should().Be(deviceId);
        savedEvent.EventType.Should().Be("EntryDetected");
        savedEvent.EventStatus.Should().Be("Success");
        savedEvent.Description.Should().Be("Vehicle detected at entry gate.");
        savedEvent.Payload.Should().Be(request.Payload);

        _deviceEventRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DeviceEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyParkingDeviceId_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest(Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Parking device ID is required*");

        _parkingDeviceRepositoryMock.Verify(
            x => x.ExistsAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenParkingDeviceDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var deviceId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*parking device was not found*");

        _deviceEventRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DeviceEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyEventType_ShouldThrowArgumentException()
    {
        var deviceId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);
        request.EventType = " ";

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Event type is required*");

        _deviceEventRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DeviceEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyEventStatus_ShouldThrowArgumentException()
    {
        var deviceId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);
        request.EventStatus = " ";

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Event status is required*");

        _deviceEventRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<DeviceEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithLongEventType_ShouldThrowArgumentException()
    {
        var deviceId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);
        request.EventType = new string('A', 101);

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Event type cannot exceed 100 characters*");
    }

    [Fact]
    public async Task CreateAsync_WithLongEventStatus_ShouldThrowArgumentException()
    {
        var deviceId = Guid.NewGuid();
        var request = CreateValidRequest(deviceId);
        request.EventStatus = new string('A', 51);

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Event status cannot exceed 50 characters*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventDoesNotExist_ShouldReturnNull()
    {
        var id = Guid.NewGuid();

        _deviceEventRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceEvent?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByParkingDeviceIdAsync_WhenDeviceDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var deviceId = Guid.NewGuid();

        _parkingDeviceRepositoryMock
            .Setup(x => x.ExistsAsync(
                deviceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = async () => await _sut.GetByParkingDeviceIdAsync(deviceId);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*parking device was not found*");

        _deviceEventRepositoryMock.Verify(
            x => x.GetByParkingDeviceIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByEventTypeAsync_WithEmptyEventType_ShouldThrowArgumentException()
    {
        var act = async () => await _sut.GetByEventTypeAsync(" ");

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Event type is required*");

        _deviceEventRepositoryMock.Verify(
            x => x.GetByEventTypeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithInvalidRange_ShouldThrowArgumentException()
    {
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(-1);

        var act = async () => await _sut.GetByDateRangeAsync(from, to);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*start date must be earlier than or equal to the end date*");

        _deviceEventRepositoryMock.Verify(
            x => x.GetByDateRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

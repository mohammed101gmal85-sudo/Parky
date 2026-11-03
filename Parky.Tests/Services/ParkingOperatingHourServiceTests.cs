
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.ParkingOperatingHour;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ParkingOperatingHourServiceTests
{
    private readonly Mock<IParkingOperatingHourRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingOperatingHourService _sut;

    public ParkingOperatingHourServiceTests()
    {
        _sut = new ParkingOperatingHourService(
            _repositoryMock.Object,
            _mapperMock.Object);

        _mapperMock
            .Setup(x => x.Map<ParkingOperatingHourResponse>(
                It.IsAny<ParkingOperatingHour>()))
            .Returns(new ParkingOperatingHourResponse());
    }

    private static CreateParkingOperatingHourRequest ValidRequest(
        Guid? parkingId = null,
        TimeOnly? openTime = null,
        TimeOnly? closeTime = null,
        bool isClosed = false,
        bool isOpen24Hours = false)
    {
        return new CreateParkingOperatingHourRequest
        {
            ParkingId = parkingId ?? Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            OpenTime = openTime ?? new TimeOnly(8, 0),
            CloseTime = closeTime ?? new TimeOnly(22, 0),
            IsClosed = isClosed,
            IsOpen24Hours = isOpen24Hours
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateOperatingHour()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();
        ParkingOperatingHour? saved = null;

        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(
                request.ParkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.DayExistsAsync(
                request.ParkingId,
                request.DayOfWeek,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<ParkingOperatingHour>(),
                It.IsAny<CancellationToken>()))
            .Callback<ParkingOperatingHour, CancellationToken>(
                (hour, _) => saved = hour)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(
            request,
            ownerId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.OpenTime.Should().Be(new TimeOnly(8, 0));
        saved.CloseTime.Should().Be(new TimeOnly(22, 0));

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingOperatingHour>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenClosedAndOpen24Hours_ShouldThrowArgumentException()
    {
        var request = ValidRequest(
            isClosed: true,
            isOpen24Hours: true);

        var act = () => _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WhenOpeningTimeIsNotBeforeClosingTime_ShouldThrowArgumentException()
    {
        var request = ValidRequest(
            openTime: new TimeOnly(22, 0),
            closeTime: new TimeOnly(8, 0));

        var act = () => _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WhenParkingDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var request = ValidRequest();

        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(
                request.ParkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should()
            .ThrowAsync<KeyNotFoundException>();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingOperatingHour>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUserDoesNotOwnParking_ShouldThrowUnauthorizedAccessException()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(
                request.ParkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(
            request,
            ownerId);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingOperatingHour>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDayAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(
                request.ParkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.DayExistsAsync(
                request.ParkingId,
                request.DayOfWeek,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(
            request,
            ownerId);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingOperatingHour>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnerOwnsParking_ShouldDeleteHour()
    {
        var ownerId = Guid.NewGuid();

        var hour = new ParkingOperatingHour
        {
            Id = Guid.NewGuid(),
            ParkingId = Guid.NewGuid()
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                hour.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hour);

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                hour.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.DeleteAsync(
                hour,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(
            hour.Id,
            ownerId);

        result.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.DeleteAsync(
                hour,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenClosedAndOpen24Hours_ShouldThrowArgumentException()
    {
        var act = () => _sut.UpdateStatusAsync(
            Guid.NewGuid(),
            new UpdateParkingOperatingHourStatusRequest
            {
                IsClosed = true,
                IsOpen24Hours = true
            },
            Guid.NewGuid());

        await act.Should()
            .ThrowAsync<ArgumentException>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.Reservation;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;
using Parky.Infrastructure.Caching;

namespace Parky.Tests.Services;

public class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepository;
    private readonly Mock<IParkingRepository> _parkingRepository;
    private readonly Mock<INotificationDispatcher> _notificationDispatcher;
    private readonly Mock<AutoMapper.IMapper> _mapper;
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly ReservationService _sut;

    public ReservationServiceTests()
    {
        _reservationRepository = new Mock<IReservationRepository>();
        _parkingRepository = new Mock<IParkingRepository>();
        _notificationDispatcher = new Mock<INotificationDispatcher>();
        _mapper = new Mock<AutoMapper.IMapper>();

        _mapper
            .Setup(x => x.Map<ReservationResponse>(
                It.IsAny<Reservation>()))
            .Returns(new ReservationResponse());

        _sut = new ReservationService(
            _reservationRepository.Object,
            _parkingRepository.Object,
            _mapper.Object,
            _notificationDispatcher.Object,
            _cacheService.Object);
    }
    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreatePendingReservation()
    {
        var userId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        var request = new CreateReservationRequest
        {
            ParkingId = parkingId,
            VehicleId = vehicleId,
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
        };

        _parkingRepository
            .Setup(x => x.GetByIdAsync(
                parkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parking
            {
                Id = parkingId
            });

        _reservationRepository
            .Setup(x => x.HasOverlappingReservationAsync(
                parkingId,
                request.StartTime,
                request.EndTime,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _reservationRepository
            .Setup(x => x.ExistsByReservationNumberAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Reservation? savedReservation = null;

        _reservationRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()))
            .Callback<Reservation, CancellationToken>(
                (reservation, _) => savedReservation = reservation)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(request, userId);

        savedReservation.Should().NotBeNull();
        savedReservation!.UserId.Should().Be(userId);
        savedReservation.ParkingId.Should().Be(parkingId);
        savedReservation.VehicleId.Should().Be(vehicleId);
        savedReservation.Status.Should().Be("Pending");
        savedReservation.ReservationNumber.Should().StartWith("PRK-");

        _reservationRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var request = new CreateReservationRequest
        {
            ParkingId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
        };

        var act = async () => await _sut.CreateAsync(
            request,
            Guid.Empty);

        await act.Should().ThrowAsync<ArgumentException>();

        _reservationRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenParkingDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var parkingId = Guid.NewGuid();

        var request = new CreateReservationRequest
        {
            ParkingId = parkingId,
            VehicleId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
        };

        _parkingRepository
            .Setup(x => x.GetByIdAsync(
                parkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parking?)null);

        var act = async () => await _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();

        _reservationRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenReservationOverlaps_ShouldThrowInvalidOperationException()
    {
        var parkingId = Guid.NewGuid();

        var request = new CreateReservationRequest
        {
            ParkingId = parkingId,
            VehicleId = Guid.NewGuid(),
            StartTime = DateTimeOffset.UtcNow.AddDays(1),
            EndTime = DateTimeOffset.UtcNow.AddDays(1).AddHours(2)
        };

        _parkingRepository
            .Setup(x => x.GetByIdAsync(
                parkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parking
            {
                Id = parkingId
            });

        _reservationRepository
            .Setup(x => x.HasOverlappingReservationAsync(
                parkingId,
                request.StartTime,
                request.EndTime,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = async () => await _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*overlapping reservation*");

        _reservationRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenReservationBelongsToAnotherUser_ShouldReturnNull()
    {
        var reservationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = ownerId,
            Status = "Pending"
        };

        _reservationRepository
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var request = new UpdateReservationRequest
        {
            StartTime = DateTimeOffset.UtcNow.AddDays(2),
            EndTime = DateTimeOffset.UtcNow.AddDays(2).AddHours(2)
        };

        var result = await _sut.UpdateAsync(
            reservationId,
            differentUserId,
            request);

        result.Should().BeNull();

        _reservationRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelAsync_WhenReservationBelongsToAnotherUser_ShouldReturnFalse()
    {
        var reservationId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = ownerId,
            Status = "Pending"
        };

        _reservationRepository
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var request = new CancelReservationRequest
        {
            Reason = "Test cancellation"
        };

        var result = await _sut.CancelAsync(
            reservationId,
            differentUserId,
            request);

        result.Should().BeFalse();

        _reservationRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<Reservation>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

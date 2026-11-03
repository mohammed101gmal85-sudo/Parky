
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.ReservationSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ReservationSpotServiceTests
{
    private readonly Mock<IReservationSpotRepository> _reservationSpotRepositoryMock = new();
    private readonly Mock<IReservationRepository> _reservationRepositoryMock = new();
    private readonly Mock<IParkingSpotRepository> _parkingSpotRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();

    private readonly ReservationSpotService _sut;

    public ReservationSpotServiceTests()
    {
        _sut = new ReservationSpotService(
            _reservationSpotRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _parkingSpotRepositoryMock.Object,
            _mapperMock.Object
        );

        _mapperMock
            .Setup(mapper => mapper.Map<ReservationSpotResponse>(
                It.IsAny<ReservationSpot>()))
            .Returns((ReservationSpot source) => new ReservationSpotResponse
            {
                Id = source.Id,
                ReservationId = source.ReservationId,
                ParkingSpotId = source.ParkingSpotId,
                PricePerHour = source.PricePerHour,
                ReservedAmount = source.ReservedAmount,
                CreatedAt = source.CreatedAt
            });
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateReservationSpot()
    {
        var reservationId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var startTime = DateTimeOffset.UtcNow;
        var endTime = startTime.AddHours(1);

        var reservation = new Reservation
        {
            Id = reservationId,
            ParkingId = parkingId,
            StartTime = startTime,
            EndTime = endTime,
            Status = "Pending"
        };

        var parkingSpot = new ParkingSpot
        {
            Id = spotId,
            ParkingFloor = new ParkingFloor
            {
                ParkingId = parkingId
            }
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _parkingSpotRepositoryMock
            .Setup(x => x.GetByIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkingSpot);

        _reservationSpotRepositoryMock
            .Setup(x => x.ExistsAsync(
                reservationId,
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _reservationSpotRepositoryMock
            .Setup(x => x.GetByParkingSpotIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReservationSpot>());

        ReservationSpot? savedReservationSpot = null;

        _reservationSpotRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()))
            .Callback<ReservationSpot, CancellationToken>(
                (item, _) => savedReservationSpot = item)
            .Returns(Task.CompletedTask);

        var request = new CreateReservationSpotRequest
        {
            ReservationId = reservationId,
            ParkingSpotId = spotId,
            PricePerHour = 10.50m
        };

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        savedReservationSpot.Should().NotBeNull();
        savedReservationSpot!.ReservationId.Should().Be(reservationId);
        savedReservationSpot.ParkingSpotId.Should().Be(spotId);
        savedReservationSpot.PricePerHour.Should().Be(10.50m);
        savedReservationSpot.ReservedAmount.Should().Be(10.50m);

        _reservationSpotRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidHourlyPrice_ShouldThrowArgumentException()
    {
        var request = new CreateReservationSpotRequest
        {
            ReservationId = Guid.NewGuid(),
            ParkingSpotId = Guid.NewGuid(),
            PricePerHour = 0
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();

        _reservationSpotRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSpotBelongsToAnotherParking_ShouldThrowInvalidOperationException()
    {
        var reservationId = Guid.NewGuid();
        var reservationParkingId = Guid.NewGuid();
        var differentParkingId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var startTime = DateTimeOffset.UtcNow.AddDays(1);

        var reservation = new Reservation
        {
            Id = reservationId,
            ParkingId = reservationParkingId,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            Status = "Pending"
        };

        var parkingSpot = new ParkingSpot
        {
            Id = spotId,
            ParkingFloor = new ParkingFloor
            {
                ParkingId = differentParkingId
            }
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _parkingSpotRepositoryMock
            .Setup(x => x.GetByIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkingSpot);

        var request = new CreateReservationSpotRequest
        {
            ReservationId = reservationId,
            ParkingSpotId = spotId,
            PricePerHour = 20m
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must belong to the reservation parking*");

        _reservationSpotRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSameSpotHasOverlappingReservation_ShouldThrowInvalidOperationException()
    {
        var reservationId = Guid.NewGuid();
        var existingReservationId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var startTime = DateTimeOffset.UtcNow.AddDays(1);

        var reservation = new Reservation
        {
            Id = reservationId,
            ParkingId = parkingId,
            StartTime = startTime,
            EndTime = startTime.AddHours(2),
            Status = "Pending"
        };

        var existingReservation = new Reservation
        {
            Id = existingReservationId,
            ParkingId = parkingId,
            StartTime = startTime.AddHours(-1),
            EndTime = startTime.AddHours(1),
            Status = "Pending"
        };

        var parkingSpot = new ParkingSpot
        {
            Id = spotId,
            ParkingFloor = new ParkingFloor
            {
                ParkingId = parkingId
            }
        };

        _reservationRepositoryMock
            .SetupSequence(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation)
            .ReturnsAsync(existingReservation);

        _parkingSpotRepositoryMock
            .Setup(x => x.GetByIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkingSpot);

        _reservationSpotRepositoryMock
            .Setup(x => x.ExistsAsync(
                reservationId,
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _reservationSpotRepositoryMock
            .Setup(x => x.GetByParkingSpotIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ReservationSpot
                {
                    ReservationId = existingReservationId
                }
            });

        var request = new CreateReservationSpotRequest
        {
            ReservationId = reservationId,
            ParkingSpotId = spotId,
            PricePerHour = 15m
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already reserved during the selected time*");

        _reservationSpotRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldRoundReservedAmountAwayFromZero()
    {
        var reservationId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var spotId = Guid.NewGuid();

        var startTime = DateTimeOffset.UtcNow.AddDays(1);

        var reservation = new Reservation
        {
            Id = reservationId,
            ParkingId = parkingId,
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Status = "Pending"
        };

        var parkingSpot = new ParkingSpot
        {
            Id = spotId,
            ParkingFloor = new ParkingFloor
            {
                ParkingId = parkingId
            }
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _parkingSpotRepositoryMock
            .Setup(x => x.GetByIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkingSpot);

        _reservationSpotRepositoryMock
            .Setup(x => x.ExistsAsync(
                reservationId,
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _reservationSpotRepositoryMock
            .Setup(x => x.GetByParkingSpotIdAsync(
                spotId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ReservationSpot>());

        ReservationSpot? savedReservationSpot = null;

        _reservationSpotRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<ReservationSpot>(),
                It.IsAny<CancellationToken>()))
            .Callback<ReservationSpot, CancellationToken>(
                (item, _) => savedReservationSpot = item)
            .Returns(Task.CompletedTask);

        var request = new CreateReservationSpotRequest
        {
            ReservationId = reservationId,
            ParkingSpotId = spotId,
            PricePerHour = 10.005m
        };

        await _sut.CreateAsync(request);

        savedReservationSpot.Should().NotBeNull();
        savedReservationSpot!.ReservedAmount.Should().Be(10.01m);
    }
}

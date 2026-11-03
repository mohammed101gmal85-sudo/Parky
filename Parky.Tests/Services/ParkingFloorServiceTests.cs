using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ParkingFloorServiceTests
{
    private readonly Mock<IParkingFloorRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingFloorService _sut;

    public ParkingFloorServiceTests()
    {
        _sut = new ParkingFloorService(_repositoryMock.Object, _mapperMock.Object);
        _mapperMock
            .Setup(x => x.Map<ParkingFloorResponse>(It.IsAny<ParkingFloor>()))
            .Returns(new ParkingFloorResponse());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateFloor()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        ParkingFloor? saved = null;
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.FloorNumberExistsAsync(parkingId, 1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ParkingFloor>(), It.IsAny<CancellationToken>()))
            .Callback<ParkingFloor, CancellationToken>((floor, _) => saved = floor)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(
            new CreateParkingFloorRequest { ParkingId = parkingId, Name = " Ground ", FloorNumber = 1 },
            ownerId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Ground");
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WhenOwnerDoesNotOwnParking_ShouldThrowInvalidOperationException()
    {
        var parkingId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(
            new CreateParkingFloorRequest { ParkingId = parkingId, Name = "Ground", FloorNumber = 1 },
            Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ParkingFloor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFloorNumberAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.FloorNumberExistsAsync(parkingId, 2, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(
            new CreateParkingFloorRequest { ParkingId = parkingId, Name = "First", FloorNumber = 2 },
            ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFloorIsInactive_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkingFloor { IsActive = false });

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveFloorsInternalAsync_WhenParkingDoesNotExist_ShouldReturnEmpty()
    {
        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.GetActiveFloorsInternalAsync(Guid.NewGuid());

        result.Should().BeEmpty();
        _repositoryMock.Verify(
            x => x.GetByParkingIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenOwnerOwnsParking_ShouldDeactivateFloor()
    {
        var floor = new ParkingFloor { Id = Guid.NewGuid(), ParkingId = Guid.NewGuid(), IsActive = true };
        var ownerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(floor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floor);
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(floor.ParkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.UpdateAsync(floor, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeactivateAsync(floor.Id, ownerId);

        result.Should().BeTrue();
        floor.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenActivatingUnderInactiveParking_ShouldThrowInvalidOperationException()
    {
        var floor = new ParkingFloor { Id = Guid.NewGuid(), ParkingId = Guid.NewGuid(), IsActive = false };
        var ownerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(floor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(floor);
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(floor.ParkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.ParkingExistsAsync(floor.ParkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.UpdateStatusAsync(
            floor.Id, new UpdateParkingFloorStatusRequest { IsActive = true }, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}


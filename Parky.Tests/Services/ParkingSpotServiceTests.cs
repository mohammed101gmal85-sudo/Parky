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
using Parky.Application.DTOs.ParkingSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;
using Microsoft.Extensions.Options;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;

namespace Parky.Tests.Services;

public class ParkingSpotServiceTests
{
    private readonly Mock<IParkingSpotRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingSpotService _sut;
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    public ParkingSpotServiceTests()
    {
        _sut = new ParkingSpotService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _cacheServiceMock.Object,
            Options.Create(new CacheOptions()));

        _mapperMock
            .Setup(x => x.Map<ParkingSpotResponse>(It.IsAny<ParkingSpot>()))
            .Returns(new ParkingSpotResponse());
    }

    private static CreateParkingSpotRequest ValidRequest(Guid? floorId = null) => new()
    {
        ParkingFloorId = floorId ?? Guid.NewGuid(),
        SpotNumber = " A-01 ",
        Description = " Near entrance ",
        XCoordinate = 10,
        YCoordinate = 20,
        Width = 2.5m,
        Height = 5m,
        ParkingSpotTypeId = null
    };

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateAvailableActiveSpot()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();
        ParkingSpot? saved = null;
        _repositoryMock
            .Setup(x => x.ParkingFloorExistsAsync(request.ParkingFloorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.ParkingFloorBelongsToOwnerAsync(request.ParkingFloorId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.SpotNumberExistsAsync(request.ParkingFloorId, "A-01", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ParkingSpot>(), It.IsAny<CancellationToken>()))
            .Callback<ParkingSpot, CancellationToken>((spot, _) => saved = spot)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request, ownerId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.SpotNumber.Should().Be("A-01");
        saved.IsActive.Should().BeTrue();
        saved.IsAvailable.Should().BeTrue();
        saved.Description.Should().Be("Near entrance");
    }

    [Fact]
    public async Task CreateAsync_WhenFloorDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var request = ValidRequest();
        _repositoryMock
            .Setup(x => x.ParkingFloorExistsAsync(request.ParkingFloorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_WhenOwnerDoesNotOwnFloor_ShouldThrowUnauthorizedAccessException()
    {
        var request = ValidRequest();
        _repositoryMock
            .Setup(x => x.ParkingFloorExistsAsync(request.ParkingFloorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.ParkingFloorBelongsToOwnerAsync(
                request.ParkingFloorId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_WhenSpotNumberAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ParkingFloorExistsAsync(request.ParkingFloorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.ParkingFloorBelongsToOwnerAsync(request.ParkingFloorId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.SpotNumberExistsAsync(request.ParkingFloorId, "A-01", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenDeactivatingSpot_ShouldAlsoMakeItUnavailable()
    {
        var ownerId = Guid.NewGuid();
        var spot = new ParkingSpot { Id = Guid.NewGuid(), ParkingFloorId = Guid.NewGuid(), IsActive = true, IsAvailable = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(spot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(spot);
        _repositoryMock
            .Setup(x => x.ParkingFloorBelongsToOwnerAsync(spot.ParkingFloorId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.UpdateAsync(spot, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateStatusAsync(
            spot.Id, new UpdateParkingSpotStatusRequest { IsActive = false }, ownerId);

        result.Should().NotBeNull();
        spot.IsActive.Should().BeFalse();
        spot.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenInactiveSpotIsMarkedAvailable_ShouldThrowInvalidOperationException()
    {
        var ownerId = Guid.NewGuid();
        var spot = new ParkingSpot { Id = Guid.NewGuid(), ParkingFloorId = Guid.NewGuid(), IsActive = false, IsAvailable = false };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(spot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(spot);
        _repositoryMock
            .Setup(x => x.ParkingFloorBelongsToOwnerAsync(spot.ParkingFloorId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.UpdateStatusAsync(
            spot.Id, new UpdateParkingSpotStatusRequest { IsAvailable = true }, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenNoStatusFieldProvided_ShouldThrowArgumentException()
    {
        var act = () => _sut.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateParkingSpotStatusRequest(), Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>();
    }
}

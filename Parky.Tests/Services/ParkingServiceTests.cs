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
using Parky.Application.DTOs.Parking;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;
using Microsoft.Extensions.Options;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;

namespace Parky.Tests.Services;

public class ParkingServiceTests
{
    private readonly Mock<IParkingRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingService _sut;
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    public ParkingServiceTests()
    {
        _sut = new ParkingService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _cacheServiceMock.Object,
            Options.Create(new CacheOptions()));

        _mapperMock
            .Setup(x => x.Map<ParkingResponse>(It.IsAny<Parking>()))
            .Returns(new ParkingResponse());
    }

    private static CreateParkingRequest ValidCreateRequest() => new()
    {
        Name = " Main Parking ",
        Description = " Secure parking ",
        Governorate = " Sohag ",
        City = " Sohag ",
        Address = " Main Street ",
        Latitude = 26,
        Longitude = 31
    };

    [Fact]
    public async Task CreateAsync_ShouldAssignOwnerAndTrimText()
    {
        var ownerId = Guid.NewGuid();
        Parking? saved = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Parking>(), It.IsAny<CancellationToken>()))
            .Callback<Parking, CancellationToken>((parking, _) => saved = parking)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(ValidCreateRequest(), ownerId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.OwnerId.Should().Be(ownerId);
        saved.Name.Should().Be("Main Parking");
        saved.Description.Should().Be("Secure parking");
        saved.Governorate.Should().Be("Sohag");
        saved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenParkingIsInactive_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parking { IsActive = false });

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WhenParkingBelongsToAnotherOwner_ShouldReturnNull()
    {
        var parking = new Parking { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid(), IsActive = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(parking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parking);

        var request = new UpdateParkingRequest
        {
            Name = "Updated",
            Description = "Description",
            Governorate = "Sohag",
            City = "Sohag",
            Address = "Address",
            Latitude = 26,
            Longitude = 31
        };

        var result = await _sut.UpdateAsync(parking.Id, Guid.NewGuid(), request);

        result.Should().BeNull();
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Parking>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenParkingIsActiveAndOwned_ShouldDeactivate()
    {
        var ownerId = Guid.NewGuid();
        var parking = new Parking { Id = Guid.NewGuid(), OwnerId = ownerId, IsActive = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(parking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parking);
        _repositoryMock
            .Setup(x => x.UpdateAsync(parking, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeactivateAsync(parking.Id, ownerId);

        result.Should().BeTrue();
        parking.IsActive.Should().BeFalse();
        _repositoryMock.Verify(
            x => x.UpdateAsync(parking, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenParkingIsInactive_ShouldReturnFalse()
    {
        var ownerId = Guid.NewGuid();
        var parking = new Parking { Id = Guid.NewGuid(), OwnerId = ownerId, IsActive = false };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(parking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parking);

        var result = await _sut.DeactivateAsync(parking.Id, ownerId);

        result.Should().BeFalse();
    }
}

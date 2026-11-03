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
using Parky.Application.DTOs.Vehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class VehicleServiceTests
{
    private readonly Mock<IVehicleRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly VehicleService _sut;

    public VehicleServiceTests()
    {
        _sut = new VehicleService(_repositoryMock.Object, _mapperMock.Object);
        _mapperMock
            .Setup(x => x.Map<VehicleResponse>(It.IsAny<Vehicle>()))
            .Returns(new VehicleResponse());
    }

    private static CreateVehicleRequest ValidRequest() => new()
    {
        PlateNumber = " 12345 ",
        PlateCode = " AB ",
        Make = " Toyota ",
        Model = " Corolla ",
        Color = " White ",
        ManufacturingYear = 2022,
        VehicleType = "Car",
        IsDefault = false
    };

    [Fact]
    public async Task CreateAsync_WhenFirstVehicle_ShouldMakeItDefault()
    {
        var userId = Guid.NewGuid();
        Vehicle? saved = null;
        _repositoryMock
            .Setup(x => x.PlateNumberExistsAsync("12345", "AB", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle>());
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Callback<Vehicle, CancellationToken>((vehicle, _) => saved = vehicle)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(ValidRequest(), userId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.IsDefault.Should().BeTrue();
        saved.PlateNumber.Should().Be("12345");
        saved.PlateCode.Should().Be("AB");
        saved.Make.Should().Be("Toyota");
    }

    [Fact]
    public async Task CreateAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var act = () => _sut.CreateAsync(ValidRequest(), Guid.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_WhenPlateAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.PlateNumberExistsAsync("12345", "AB", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(ValidRequest(), userId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenNewVehicleIsDefault_ShouldUnsetExistingDefault()
    {
        var userId = Guid.NewGuid();
        var existing = new Vehicle { Id = Guid.NewGuid(), IsDefault = true };
        _repositoryMock
            .Setup(x => x.PlateNumberExistsAsync("12345", "AB", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle> { existing });
        _repositoryMock
            .Setup(x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var request = ValidRequest();
        request.IsDefault = true;

        await _sut.CreateAsync(request, userId);

        existing.IsDefault.Should().BeFalse();
        _repositoryMock.Verify(
            x => x.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenVehicleBelongsToAnotherUser_ShouldReturnNull()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsActive = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var result = await _sut.UpdateAsync(
            vehicle.Id, Guid.NewGuid(), new UpdateVehicleRequest());

        result.Should().BeNull();
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenDefaultVehicleHasReplacement_ShouldPromoteReplacement()
    {
        var userId = Guid.NewGuid();
        var current = new Vehicle { Id = Guid.NewGuid(), UserId = userId, IsActive = true, IsDefault = true };
        var replacement = new Vehicle { Id = Guid.NewGuid(), UserId = userId, IsActive = true, IsDefault = false, CreatedAt = DateTimeOffset.UtcNow };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(current.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        _repositoryMock
            .Setup(x => x.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Vehicle> { current, replacement });
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeactivateAsync(current.Id, userId);

        result.Should().BeTrue();
        current.IsActive.Should().BeFalse();
        current.IsDefault.Should().BeFalse();
        replacement.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAsync_WhenVehicleBelongsToAnotherUser_ShouldReturnNull()
    {
        var vehicle = new Vehicle { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsActive = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(vehicle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        var result = await _sut.SetDefaultAsync(vehicle.Id, Guid.NewGuid());

        result.Should().BeNull();
    }
}


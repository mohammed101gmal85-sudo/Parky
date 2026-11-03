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
using Parky.Application.DTOs.UserVehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class UserVehicleServiceTests
{
    private readonly Mock<IUserVehicleRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly UserVehicleService _sut;

    public UserVehicleServiceTests()
    {
        _sut = new UserVehicleService(_repositoryMock.Object, _mapperMock.Object);
        _mapperMock
            .Setup(x => x.Map<UserVehicleResponse>(It.IsAny<UserVehicle>()))
            .Returns(new UserVehicleResponse());
    }

    private static CreateUserVehicleRequest ValidRequest(Guid? userId = null, Guid? vehicleId = null) => new()
    {
        UserId = userId ?? Guid.NewGuid(),
        VehicleId = vehicleId ?? Guid.NewGuid(),
        IsDefault = false
    };

    [Fact]
    public async Task CreateAsync_WhenFirstLinkedVehicle_ShouldMakeItDefault()
    {
        var request = ValidRequest();
        UserVehicle? saved = null;
        _repositoryMock
            .Setup(x => x.ExistsAsync(request.UserId, request.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(request.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserVehicle>());
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserVehicle>(), It.IsAny<CancellationToken>()))
            .Callback<UserVehicle, CancellationToken>((link, _) => saved = link)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.IsDefault.Should().BeTrue();
        saved.UserId.Should().Be(request.UserId);
        saved.VehicleId.Should().Be(request.VehicleId);
    }

    [Fact]
    public async Task CreateAsync_WhenAlreadyLinked_ShouldThrowInvalidOperationException()
    {
        var request = ValidRequest();
        _repositoryMock
            .Setup(x => x.ExistsAsync(request.UserId, request.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserVehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyVehicleId_ShouldThrowArgumentException()
    {
        var request = ValidRequest(vehicleId: Guid.Empty);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenRemovingCurrentDefault_ShouldThrowInvalidOperationException()
    {
        var link = new UserVehicle { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), IsDefault = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(link.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);
        _repositoryMock
            .Setup(x => x.GetDefaultByUserIdAsync(link.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        var act = () => _sut.UpdateAsync(
            link.Id, new UpdateUserVehicleRequest { IsDefault = false });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SetDefaultAsync_ShouldUnsetPreviousDefaultAndSetTarget()
    {
        var userId = Guid.NewGuid();
        var target = new UserVehicle { Id = Guid.NewGuid(), UserId = userId, VehicleId = Guid.NewGuid(), IsDefault = false };
        var previous = new UserVehicle { Id = Guid.NewGuid(), UserId = userId, IsDefault = true };
        _repositoryMock
            .Setup(x => x.GetByUserAndVehicleAsync(userId, target.VehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserVehicle> { previous, target });
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<UserVehicle>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SetDefaultAsync(userId, target.VehicleId);

        result.Should().NotBeNull();
        previous.IsDefault.Should().BeFalse();
        target.IsDefault.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<UserVehicle>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletingDefault_ShouldPromoteMostRecentlyCreatedVehicle()
    {
        var userId = Guid.NewGuid();
        var current = new UserVehicle { Id = Guid.NewGuid(), UserId = userId, IsDefault = true };
        var older = new UserVehicle { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) };
        var newer = new UserVehicle { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(current.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current);
        _repositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserVehicle> { current, older, newer });
        _repositoryMock
            .Setup(x => x.UpdateAsync(newer, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(x => x.DeleteAsync(current, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(current.Id);

        result.Should().BeTrue();
        newer.IsDefault.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.DeleteAsync(current, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenLinkDoesNotExist_ShouldReturnFalse()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVehicle?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }
}

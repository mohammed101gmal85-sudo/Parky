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
using Parky.Application.DTOs.Favorite;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class FavoriteServiceTests
{
    private readonly Mock<IFavoriteRepository> _favoriteRepositoryMock = new();
    private readonly Mock<IParkingRepository> _parkingRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly FavoriteService _sut;

    public FavoriteServiceTests()
    {
        _sut = new FavoriteService(
            _favoriteRepositoryMock.Object,
            _parkingRepositoryMock.Object,
            _mapperMock.Object);

        _mapperMock
            .Setup(x => x.Map<FavoriteResponse>(It.IsAny<Favorite>()))
            .Returns(new FavoriteResponse());
    }

    [Fact]
    public async Task AddAsync_WithValidData_ShouldCreateFavorite()
    {
        var userId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        Favorite? savedFavorite = null;

        _parkingRepositoryMock
            .Setup(x => x.ExistsAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _favoriteRepositoryMock
            .Setup(x => x.ExistsAsync(userId, parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _favoriteRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Favorite>(), It.IsAny<CancellationToken>()))
            .Callback<Favorite, CancellationToken>((favorite, _) => savedFavorite = favorite)
            .Returns(Task.CompletedTask);

        var result = await _sut.AddAsync(
            new CreateFavoriteRequest { ParkingId = parkingId }, userId);

        result.Should().NotBeNull();
        savedFavorite.Should().NotBeNull();
        savedFavorite!.UserId.Should().Be(userId);
        savedFavorite.ParkingId.Should().Be(parkingId);
        _favoriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Favorite>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_WhenParkingDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var parkingId = Guid.NewGuid();
        _parkingRepositoryMock
            .Setup(x => x.ExistsAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.AddAsync(
            new CreateFavoriteRequest { ParkingId = parkingId }, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _favoriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Favorite>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenAlreadyFavorite_ShouldThrowInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        _parkingRepositoryMock
            .Setup(x => x.ExistsAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _favoriteRepositoryMock
            .Setup(x => x.ExistsAsync(userId, parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.AddAsync(
            new CreateFavoriteRequest { ParkingId = parkingId }, userId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _favoriteRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Favorite>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        var act = () => _sut.AddAsync(
            new CreateFavoriteRequest { ParkingId = Guid.NewGuid() }, Guid.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddAsync_WithEmptyParkingId_ShouldThrowArgumentException()
    {
        var act = () => _sut.AddAsync(
            new CreateFavoriteRequest { ParkingId = Guid.Empty }, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>();
        _parkingRepositoryMock.Verify(
            x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenFavoriteExists_ShouldDeleteFavorite()
    {
        var userId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var favorite = new Favorite { Id = Guid.NewGuid(), UserId = userId, ParkingId = parkingId };
        _favoriteRepositoryMock
            .Setup(x => x.GetByUserAndParkingAsync(userId, parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(favorite);
        _favoriteRepositoryMock
            .Setup(x => x.DeleteAsync(favorite, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _sut.RemoveAsync(parkingId, userId);

        _favoriteRepositoryMock.Verify(
            x => x.DeleteAsync(favorite, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenFavoriteDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _favoriteRepositoryMock
            .Setup(x => x.GetByUserAndParkingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Favorite?)null);

        var act = () => _sut.RemoveAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        _favoriteRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<Favorite>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IsFavoriteAsync_ShouldReturnRepositoryResult()
    {
        var userId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        _favoriteRepositoryMock
            .Setup(x => x.ExistsAsync(userId, parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.IsFavoriteAsync(parkingId, userId);

        result.Should().BeTrue();
    }
}

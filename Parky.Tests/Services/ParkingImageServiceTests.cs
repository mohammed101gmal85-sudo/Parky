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
using Parky.Application.DTOs.ParkingImage;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ParkingImageServiceTests
{
    private readonly Mock<IParkingImageRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();
    private readonly ParkingImageService _sut;

    public ParkingImageServiceTests()
    {
        _sut = new ParkingImageService(
            _repositoryMock.Object,
            _mapperMock.Object,
            _fileStorageServiceMock.Object);

        _mapperMock
            .Setup(x => x.Map<ParkingImageResponse>(It.IsAny<ParkingImage>()))
            .Returns(new ParkingImageResponse());
        _mapperMock
            .Setup(x => x.Map<List<ParkingImageResponse>>(It.IsAny<List<ParkingImage>>()))
            .Returns(new List<ParkingImageResponse>());
    }

    private static UpdateParkingImagePrimaryRequest PrimaryRequest(bool isPrimary = true) => new()
    {
        IsPrimary = isPrimary
    };

    [Fact]
    public async Task CreateAsync_WithEmptyParkingId_ShouldThrowArgumentException()
    {
        var request = new CreateParkingImageRequest
        {
            ParkingId = Guid.Empty,
            DisplayOrder = 0
        };

        var act = () => _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(
            x => x.ParkingBelongsToOwnerAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidDisplayOrder_ShouldThrowArgumentOutOfRangeException()
    {
        var request = new CreateParkingImageRequest
        {
            ParkingId = Guid.NewGuid(),
            DisplayOrder = -1
        };

        var act = () => _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CreateAsync_WhenOwnerDoesNotOwnParking_ShouldThrowInvalidOperationException()
    {
        var request = new CreateParkingImageRequest
        {
            ParkingId = Guid.NewGuid(),
            DisplayOrder = 0
        };
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_WhenDisplayOrderAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var request = new CreateParkingImageRequest
        {
            ParkingId = Guid.NewGuid(),
            DisplayOrder = 3
        };
        var ownerId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.DisplayOrderExistsAsync(
                request.ParkingId, request.DisplayOrder, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdatePrimaryStatusAsync_WhenMakingImagePrimary_ShouldClearOtherPrimaryImages()
    {
        var ownerId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var target = new ParkingImage
        {
            Id = Guid.NewGuid(),
            ParkingId = parkingId,
            IsPrimary = false
        };
        var previousPrimary = new ParkingImage
        {
            Id = Guid.NewGuid(),
            ParkingId = parkingId,
            IsPrimary = true
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.GetByParkingIdAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkingImage> { target, previousPrimary });
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ParkingImage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdatePrimaryStatusAsync(
            target.Id, PrimaryRequest(), ownerId);

        result.Should().NotBeNull();
        previousPrimary.IsPrimary.Should().BeFalse();
        target.IsPrimary.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.UpdateAsync(previousPrimary, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(
            x => x.UpdateAsync(target, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenImageIsPrimary_ShouldPromoteLowestOrderedRemainingImage()
    {
        var ownerId = Guid.NewGuid();
        var parkingId = Guid.NewGuid();
        var image = new ParkingImage
        {
            Id = Guid.NewGuid(),
            ParkingId = parkingId,
            IsPrimary = true,
            PublicId = "remote-image-id"
        };
        var second = new ParkingImage
        {
            Id = Guid.NewGuid(),
            ParkingId = parkingId,
            DisplayOrder = 2,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        };
        var first = new ParkingImage
        {
            Id = Guid.NewGuid(),
            ParkingId = parkingId,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(image.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.DeleteAsync(image, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock
            .Setup(x => x.GetByParkingIdAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkingImage> { second, first });
        _repositoryMock
            .Setup(x => x.UpdateAsync(first, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(image.Id, ownerId);

        result.Should().BeTrue();
        first.IsPrimary.Should().BeTrue();
        _repositoryMock.Verify(
            x => x.DeleteAsync(image, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(
            x => x.UpdateAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        _fileStorageServiceMock.Verify(
            x => x.DeleteImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenImageDoesNotExist_ShouldReturnFalse()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkingImage?)null);

        var result = await _sut.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_WithDuplicateIds_ShouldThrowInvalidOperationException()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.ReorderAsync(
            parkingId, new[] { imageId, imageId }, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ParkingImage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReorderAsync_WhenIdsDoNotMatchParkingImages_ShouldThrowInvalidOperationException()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var currentImage = new ParkingImage { Id = Guid.NewGuid(), ParkingId = parkingId };
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.GetByParkingIdAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkingImage> { currentImage });

        var act = () => _sut.ReorderAsync(
            parkingId, new[] { Guid.NewGuid() }, ownerId);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReorderAsync_WithValidIds_ShouldUpdateDisplayOrders()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var first = new ParkingImage { Id = Guid.NewGuid(), ParkingId = parkingId, DisplayOrder = 5 };
        var second = new ParkingImage { Id = Guid.NewGuid(), ParkingId = parkingId, DisplayOrder = 8 };
        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId, ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.GetByParkingIdAsync(parkingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkingImage> { first, second });
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ParkingImage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ReorderAsync(
            parkingId, new[] { second.Id, first.Id }, ownerId);

        second.DisplayOrder.Should().Be(0);
        first.DisplayOrder.Should().Be(1);
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ParkingImage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        result.Should().NotBeNull();
    }
}

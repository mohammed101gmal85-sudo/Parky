
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.ParkingGate;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ParkingGateServiceTests
{
    private readonly Mock<IParkingGateRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingGateService _sut;

    public ParkingGateServiceTests()
    {
        _sut = new ParkingGateService(
            _repositoryMock.Object,
            _mapperMock.Object);

        _mapperMock
            .Setup(x => x.Map<ParkingGateResponse>(
                It.IsAny<ParkingGate>()))
            .Returns(new ParkingGateResponse());

        _mapperMock
            .Setup(x => x.Map<List<ParkingGateResponse>>(
                It.IsAny<List<ParkingGate>>()))
            .Returns(new List<ParkingGateResponse>());
    }

    private static CreateParkingGateRequest ValidRequest(
        Guid? parkingId = null,
        bool isEntryGate = true,
        bool isExitGate = false)
    {
        return new CreateParkingGateRequest
        {
            ParkingId = parkingId ?? Guid.NewGuid(),
            Name = " Main Gate ",
            GateCode = " GATE-01 ",
            GateType = "Automatic",
            LocationDescription = " North entrance ",
            IsEntryGate = isEntryGate,
            IsExitGate = isExitGate
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateGateAndNormalizeText()
    {
        var parkingId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        ParkingGate? saved = null;

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                parkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.GateCodeExistsAsync(
                "GATE-01",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<ParkingGate>(),
                It.IsAny<CancellationToken>()))
            .Callback<ParkingGate, CancellationToken>(
                (gate, _) => saved = gate)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(
            ValidRequest(parkingId),
            ownerId);

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.GateCode.Should().Be("GATE-01");
        saved.Name.Should().Be("Main Gate");
        saved.LocationDescription.Should().Be("North entrance");
        saved.IsActive.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingGate>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenGateDirectionIsInvalid_ShouldThrowInvalidOperationException()
    {
        var request = ValidRequest(
            isEntryGate: false,
            isExitGate: false);

        var act = () => _sut.CreateAsync(
            request,
            Guid.NewGuid());

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingGate>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenGateCodeExists_ShouldThrowInvalidOperationException()
    {
        var request = ValidRequest();
        var ownerId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.GateCodeExistsAsync(
                "GATE-01",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request, ownerId);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        _repositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<ParkingGate>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyOwnerId_ShouldThrowArgumentException()
    {
        var act = () => _sut.CreateAsync(
            ValidRequest(),
            Guid.Empty);

        await act.Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAllAsync_WithoutPagination_ShouldReturnOnlyActiveGates()
    {
        _repositoryMock
            .Setup(x => x.GetAllAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkingGate>
            {
                new() { IsActive = true },
                new() { IsActive = false }
            });

        _mapperMock
            .Setup(x => x.Map<List<ParkingGateResponse>>(
                It.Is<List<ParkingGate>>(items => items.Count == 1)))
            .Returns(new List<ParkingGateResponse>
            {
                new()
            });

        var result = await _sut.GetAllAsync(
            CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeactivateAsync_WhenOwnerDoesNotOwnParking_ShouldThrowInvalidOperationException()
    {
        var gate = new ParkingGate
        {
            Id = Guid.NewGuid(),
            ParkingId = Guid.NewGuid(),
            IsActive = true
        };

        var ownerId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                gate.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(gate);

        _repositoryMock
            .Setup(x => x.ParkingBelongsToOwnerAsync(
                gate.ParkingId,
                ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.DeactivateAsync(
            gate.Id,
            ownerId);

        await act.Should()
            .ThrowAsync<InvalidOperationException>();

        _repositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<ParkingGate>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenGateDoesNotExist_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkingGate?)null);

        var result = await _sut.UpdateStatusAsync(
            Guid.NewGuid(),
            new UpdateParkingGateStatusRequest
            {
                IsActive = true
            },
            Guid.NewGuid());

        result.Should().BeNull();
    }
}

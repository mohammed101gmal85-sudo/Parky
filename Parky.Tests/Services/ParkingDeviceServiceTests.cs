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
using Parky.Application.DTOs.ParkingDevice;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class ParkingDeviceServiceTests
{
    private readonly Mock<IParkingDeviceRepository> _repositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly ParkingDeviceService _sut;

    public ParkingDeviceServiceTests()
    {
        _sut = new ParkingDeviceService(_repositoryMock.Object, _mapperMock.Object);
        _mapperMock
            .Setup(x => x.Map<ParkingDeviceResponse>(It.IsAny<ParkingDevice>()))
            .Returns(new ParkingDeviceResponse());
    }

    private static CreateParkingDeviceRequest ValidCreateRequest(Guid? gateId = null) => new()
    {
        ParkingGateId = gateId ?? Guid.NewGuid(),
        Name = "Entry Controller",
        DeviceCode = " DEV-001 ",
        DeviceType = "EntryScanner",
        SerialNumber = " SERIAL-01 ",
        IpAddress = " 192.168.1.10 "
    };

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateDeviceAndNormalizeValues()
    {
        var gateId = Guid.NewGuid();
        ParkingDevice? saved = null;
        _repositoryMock
            .Setup(x => x.ParkingGateExistsAsync(gateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.DeviceCodeExistsAsync("DEV-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ParkingDevice>(), It.IsAny<CancellationToken>()))
            .Callback<ParkingDevice, CancellationToken>((device, _) => saved = device)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(ValidCreateRequest(gateId));

        result.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved!.DeviceCode.Should().Be("DEV-001");
        saved.Name.Should().Be("Entry Controller");
        saved.SerialNumber.Should().Be("SERIAL-01");
        saved.IpAddress.Should().Be("192.168.1.10");
        saved.IsActive.Should().BeTrue();
        saved.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WhenGateDoesNotExist_ShouldThrowInvalidOperationException()
    {
        var request = ValidCreateRequest();
        _repositoryMock
            .Setup(x => x.ParkingGateExistsAsync(request.ParkingGateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ParkingDevice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenDeviceCodeAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var request = ValidCreateRequest();
        _repositoryMock
            .Setup(x => x.ParkingGateExistsAsync(request.ParkingGateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(x => x.DeviceCodeExistsAsync("DEV-001", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ParkingDevice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDeviceIsInactive_ShouldReturnNull()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkingDevice { IsActive = false });

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateAsync_WhenDeviceIsActive_ShouldDeactivateAndUpdate()
    {
        var device = new ParkingDevice { IsActive = true, IsOnline = true };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _repositoryMock
            .Setup(x => x.UpdateAsync(device, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.DeactivateAsync(Guid.NewGuid());

        result.Should().BeTrue();
        device.IsActive.Should().BeFalse();
        device.IsOnline.Should().BeFalse();
        _repositoryMock.Verify(
            x => x.UpdateAsync(device, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenDeviceDoesNotExist_ShouldReturnFalse()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkingDevice?)null);

        var result = await _sut.DeactivateAsync(Guid.NewGuid());

        result.Should().BeFalse();
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<ParkingDevice>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenDeviceIsOnline_ShouldSetLastSeenAt()
    {
        var device = new ParkingDevice { IsActive = true, IsOnline = false };
        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);
        _repositoryMock
            .Setup(x => x.UpdateAsync(device, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpdateStatusAsync(
            Guid.NewGuid(), new UpdateParkingDeviceStatusRequest { IsOnline = true });

        result.Should().NotBeNull();
        device.IsOnline.Should().BeTrue();
        device.LastSeenAt.Should().NotBeNull();
    }
}

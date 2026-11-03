using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.QRCode;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class QRCodeServiceTests
{
    private readonly Mock<IQRCodeRepository> _qrCodeRepositoryMock = new();
    private readonly Mock<IReservationRepository> _reservationRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<INotificationDispatcher> _notificationDispatcherMock = new();

    private readonly QRCodeService _sut;

    public QRCodeServiceTests()
    {
        _sut = new QRCodeService(
            _qrCodeRepositoryMock.Object,
            _reservationRepositoryMock.Object,
            _mapperMock.Object,
            _notificationDispatcherMock.Object
        );

        _mapperMock
            .Setup(x => x.Map<QRCodeResponse>(
                It.IsAny<QRCode>()))
            .Returns(new QRCodeResponse());
    }

    private static string ComputeHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldCreateQRCode()
    {
        var reservationId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = Guid.NewGuid(),
            ReservationNumber = "RES-1001"
        };

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _qrCodeRepositoryMock
            .Setup(x => x.HasActiveCodeForReservationAsync(
                reservationId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        QRCode? savedQRCode = null;

        _qrCodeRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()))
            .Callback<QRCode, CancellationToken>(
                (qrCode, _) => savedQRCode = qrCode)
            .Returns(Task.CompletedTask);

        var request = new CreateQRCodeRequest
        {
            ReservationId = reservationId,
            ValidityMinutes = 30
        };

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.QRCode.Should().NotBeNull();

        savedQRCode.Should().NotBeNull();
        savedQRCode!.ReservationId.Should().Be(reservationId);
        savedQRCode.TokenHash.Should().NotBe(result.Token);
        savedQRCode.IsRevoked.Should().BeFalse();
        savedQRCode.UsageCount.Should().Be(0);
        savedQRCode.ExpiresAt.Should().BeAfter(savedQRCode.CreatedAt);

        _qrCodeRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyReservationId_ShouldThrowArgumentException()
    {
        var request = new CreateQRCodeRequest
        {
            ReservationId = Guid.Empty,
            ValidityMinutes = 30
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*ReservationId is required*");

        _qrCodeRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(721)]
    public async Task CreateAsync_WithInvalidValidityMinutes_ShouldThrowArgumentException(
        int validityMinutes)
    {
        var request = new CreateQRCodeRequest
        {
            ReservationId = Guid.NewGuid(),
            ValidityMinutes = validityMinutes
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*ValidityMinutes must be between 1 and 720*");
    }

    [Fact]
    public async Task CreateAsync_WhenReservationDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var request = new CreateQRCodeRequest
        {
            ReservationId = reservationId,
            ValidityMinutes = 30
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Reservation was not found*");

        _qrCodeRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenActiveQRCodeAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var reservationId = Guid.NewGuid();

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = reservationId,
                UserId = Guid.NewGuid(),
                ReservationNumber = "RES-1002"
            });

        _qrCodeRepositoryMock
            .Setup(x => x.HasActiveCodeForReservationAsync(
                reservationId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new CreateQRCodeRequest
        {
            ReservationId = reservationId,
            ValidityMinutes = 30
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active QR code*");

        _qrCodeRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAndUseAsync_WithValidToken_ShouldMarkQRCodeAsUsed()
    {
        const string token = "test-valid-qr-token";
        var reservationId = Guid.NewGuid();

        var qrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = reservationId,
            TokenHash = ComputeHash(token),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            IsRevoked = false,
            UsedAt = null,
            UsageCount = 0
        };

        _qrCodeRepositoryMock
            .Setup(x => x.GetByTokenHashAsync(
                ComputeHash(token),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(qrCode);

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = reservationId,
                UserId = Guid.NewGuid(),
                ReservationNumber = "RES-1003"
            });

        _qrCodeRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.ValidateAndUseAsync(
            new ValidateQRCodeRequest
            {
                Token = token
            });

        result.Should().NotBeNull();
        qrCode.UsedAt.Should().NotBeNull();
        qrCode.UsageCount.Should().Be(1);

        _qrCodeRepositoryMock.Verify(
            x => x.UpdateAsync(
                qrCode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAndUseAsync_WithEmptyToken_ShouldReturnNull()
    {
        var result = await _sut.ValidateAndUseAsync(
            new ValidateQRCodeRequest
            {
                Token = " "
            });

        result.Should().BeNull();

        _qrCodeRepositoryMock.Verify(
            x => x.GetByTokenHashAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAndUseAsync_WhenQRCodeDoesNotExist_ShouldReturnNull()
    {
        const string token = "unknown-token";

        var result = await _sut.ValidateAndUseAsync(
            new ValidateQRCodeRequest
            {
                Token = token
            });

        result.Should().BeNull();

        _qrCodeRepositoryMock.Verify(
            x => x.UpdateAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAndUseAsync_WhenQRCodeIsRevoked_ShouldReturnNull()
    {
        const string token = "revoked-token";

        var qrCode = new QRCode
        {
            Id = Guid.NewGuid(),
            ReservationId = Guid.NewGuid(),
            TokenHash = ComputeHash(token),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            IsRevoked = true,
            UsageCount = 0
        };

        _qrCodeRepositoryMock
            .Setup(x => x.GetByTokenHashAsync(
                ComputeHash(token),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(qrCode);

        var result = await _sut.ValidateAndUseAsync(
            new ValidateQRCodeRequest
            {
                Token = token
            });

        result.Should().BeNull();
        qrCode.UsageCount.Should().Be(0);
    }

    [Fact]
    public async Task RevokeAsync_WithValidRequest_ShouldRevokeQRCode()
    {
        var qrCodeId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var qrCode = new QRCode
        {
            Id = qrCodeId,
            ReservationId = reservationId,
            TokenHash = "HASH",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            IsRevoked = false
        };

        _qrCodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                qrCodeId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(qrCode);

        _qrCodeRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<QRCode>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _reservationRepositoryMock
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Reservation
            {
                Id = reservationId,
                UserId = Guid.NewGuid(),
                ReservationNumber = "RES-1004"
            });

        var result = await _sut.RevokeAsync(
            qrCodeId,
            new RevokeQRCodeRequest
            {
                Reason = "Reservation cancelled."
            });

        result.Should().BeTrue();
        qrCode.IsRevoked.Should().BeTrue();

        _qrCodeRepositoryMock.Verify(
            x => x.UpdateAsync(
                qrCode,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RevokeAsync_WithEmptyReason_ShouldThrowArgumentException()
    {
        var act = async () => await _sut.RevokeAsync(
            Guid.NewGuid(),
            new RevokeQRCodeRequest
            {
                Reason = " "
            });

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*revocation reason is required*");

        _qrCodeRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeAsync_WhenQRCodeDoesNotExist_ShouldReturnFalse()
    {
        var id = Guid.NewGuid();

        _qrCodeRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((QRCode?)null);

        var result = await _sut.RevokeAsync(
            id,
            new RevokeQRCodeRequest
            {
                Reason = "Manual revocation."
            });

        result.Should().BeFalse();
    }
}

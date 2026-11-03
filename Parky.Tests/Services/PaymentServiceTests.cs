using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.Payment;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _paymentRepository;
    private readonly Mock<IReservationRepository> _reservationRepository;
    private readonly Mock<AutoMapper.IMapper> _mapper;
    private readonly Mock<INotificationDispatcher> _notificationDispatcher;

    private readonly PaymentService _sut;

    public PaymentServiceTests()
    {
        _paymentRepository = new Mock<IPaymentRepository>();
        _reservationRepository = new Mock<IReservationRepository>();
        _mapper = new Mock<AutoMapper.IMapper>();
        _notificationDispatcher = new Mock<INotificationDispatcher>();

        _mapper
            .Setup(x => x.Map<PaymentResponse>(
                It.IsAny<Payment>()))
            .Returns(new PaymentResponse());

        _sut = new PaymentService(
            _paymentRepository.Object,
            _reservationRepository.Object,
            _mapper.Object,
            _notificationDispatcher.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreatePendingPayment()
    {
        var reservationId = Guid.NewGuid();

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = Guid.NewGuid()
        };

        _reservationRepository
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        Payment? savedPayment = null;

        _paymentRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Callback<Payment, CancellationToken>(
                (payment, _) => savedPayment = payment)
            .Returns(Task.CompletedTask);

        var request = new CreatePaymentRequest
        {
            ReservationId = reservationId,
            PaymentNumber = "PAY-UNIT-001",
            Amount = 150m,
            Currency = "EGP",
            PaymentMethod = "Card",
            PaymentProvider = "TestProvider"
        };

        await _sut.CreateAsync(request);

        savedPayment.Should().NotBeNull();
        savedPayment!.ReservationId.Should().Be(reservationId);
        savedPayment.Amount.Should().Be(150m);
        savedPayment.Currency.Should().Be("EGP");
        savedPayment.PaymentMethod.Should().Be("Card");
        savedPayment.PaymentStatus.Should().Be("Pending");

        _paymentRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithZeroAmount_ShouldThrowArgumentException()
    {
        var request = new CreatePaymentRequest
        {
            ReservationId = Guid.NewGuid(),
            PaymentNumber = "PAY-UNIT-002",
            Amount = 0,
            Currency = "EGP",
            PaymentMethod = "Card"
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();

        _paymentRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithUnsupportedPaymentMethod_ShouldThrowArgumentException()
    {
        var request = new CreatePaymentRequest
        {
            ReservationId = Guid.NewGuid(),
            PaymentNumber = "PAY-UNIT-003",
            Amount = 100m,
            Currency = "EGP",
            PaymentMethod = "UnknownMethod"
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();

        _paymentRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenReservationDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var reservationId = Guid.NewGuid();

        _reservationRepository
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reservation?)null);

        var request = new CreatePaymentRequest
        {
            ReservationId = reservationId,
            PaymentNumber = "PAY-UNIT-004",
            Amount = 100m,
            Currency = "EGP",
            PaymentMethod = "Card"
        };

        var act = async () => await _sut.CreateAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();

        _paymentRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTryingToSetSucceededDirectly_ShouldThrowInvalidOperationException()
    {
        var paymentId = Guid.NewGuid();

        var payment = new Payment
        {
            Id = paymentId,
            ReservationId = Guid.NewGuid(),
            PaymentStatus = "Pending"
        };

        _paymentRepository
            .Setup(x => x.GetByIdAsync(
                paymentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var request = new UpdatePaymentStatusRequest
        {
            PaymentStatus = "Succeeded"
        };

        var act = async () => await _sut.UpdateStatusAsync(
            paymentId,
            request);

        await act.Should().ThrowAsync<InvalidOperationException>();

        _paymentRepository.Verify(
            x => x.UpdateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_FromPendingToProcessing_ShouldUpdatePayment()
    {
        var paymentId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();

        var payment = new Payment
        {
            Id = paymentId,
            ReservationId = reservationId,
            PaymentStatus = "Pending"
        };

        var reservation = new Reservation
        {
            Id = reservationId,
            UserId = Guid.NewGuid()
        };

        _paymentRepository
            .Setup(x => x.GetByIdAsync(
                paymentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _reservationRepository
            .Setup(x => x.GetByIdAsync(
                reservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        _paymentRepository
            .Setup(x => x.UpdateAsync(
                It.IsAny<Payment>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = new UpdatePaymentStatusRequest
        {
            PaymentStatus = "Processing",
            ProviderTransactionId = "TXN-UNIT-001"
        };

        await _sut.UpdateStatusAsync(paymentId, request);

        payment.PaymentStatus.Should().Be("Processing");
        payment.ProviderTransactionId.Should().Be("TXN-UNIT-001");

        _paymentRepository.Verify(
            x => x.UpdateAsync(
                payment,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

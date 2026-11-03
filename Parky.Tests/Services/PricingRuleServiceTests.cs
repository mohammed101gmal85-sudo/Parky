
using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using Parky.Application.DTOs.PricingRule;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Services;
using Xunit;
using Microsoft.Extensions.Options;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;
namespace Parky.Tests.Services;

public class PricingRuleServiceTests
{
    private readonly Mock<IPricingRuleRepository> _pricingRuleRepositoryMock = new();
    private readonly Mock<IParkingRepository> _parkingRepositoryMock = new();
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly PricingRuleService _sut;

    public PricingRuleServiceTests()
    {
        _sut = new PricingRuleService(
            _pricingRuleRepositoryMock.Object,
            _parkingRepositoryMock.Object,
            _mapperMock.Object,
            _cacheServiceMock.Object,
            Options.Create(new CacheOptions()));

        _mapperMock
            .Setup(x => x.Map<PricingRuleResponse>(
                It.IsAny<PricingRule>()))
            .Returns(new PricingRuleResponse());
    }

    private static CreatePricingRuleRequest CreateValidRequest(
        Guid? parkingId = null)
    {
        return new CreatePricingRuleRequest
        {
            ParkingId = parkingId ?? Guid.NewGuid(),
            Name = "Standard Hourly Rate",
            PricingType = "Hourly",
            PricePerHour = 15m,
            Priority = 1
        };
    }

    [Fact]
    public async Task CreateAsync_WithValidHourlyRule_ShouldCreatePricingRule()
    {
        var parkingId = Guid.NewGuid();
        var request = CreateValidRequest(parkingId);

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(
                parkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Parking
            {
                Id = parkingId
            });

        PricingRule? savedRule = null;

        _pricingRuleRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()))
            .Callback<PricingRule, CancellationToken>(
                (rule, _) => savedRule = rule)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        savedRule.Should().NotBeNull();

        savedRule!.ParkingId.Should().Be(parkingId);
        savedRule.Name.Should().Be("Standard Hourly Rate");
        savedRule.PricingType.Should().Be("Hourly");
        savedRule.PricePerHour.Should().Be(15m);
        savedRule.IsActive.Should().BeTrue();

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithUnsupportedPricingType_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();
        request.PricingType = "Weekly";

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported pricing type*");

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithZeroHourlyPrice_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();
        request.PricePerHour = 0;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Hourly pricing requires a positive PricePerHour*");

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenParkingDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        var request = CreateValidRequest();

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(
                request.ParkingId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parking?)null);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Parking was not found*");

        _pricingRuleRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyParkingId_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest(Guid.Empty);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Parking ID is required*");

        _parkingRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ShouldThrowArgumentException()
    {
        var parkingId = Guid.NewGuid();
        var request = CreateValidRequest(parkingId);
        request.Name = " ";

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Rule name is required*");

        _parkingRepositoryMock.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidEffectiveDateRange_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();

        request.EffectiveFrom = DateTimeOffset.UtcNow.AddDays(2);
        request.EffectiveTo = DateTimeOffset.UtcNow.AddDays(1);

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*EffectiveTo must be later than EffectiveFrom*");
    }

    [Fact]
    public async Task CreateAsync_WithFlatPricingAndMissingFlatPrice_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();

        request.PricingType = "Flat";
        request.PricePerHour = 0;
        request.FlatPrice = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Flat pricing requires a positive FlatPrice*");
    }

    [Fact]
    public async Task CreateAsync_WithTimeBasedPricingAndMissingTimeRange_ShouldThrowArgumentException()
    {
        var request = CreateValidRequest();

        request.PricingType = "TimeBased";
        request.PricePerHour = 20m;
        request.StartTime = null;
        request.EndTime = null;

        var act = async () => await _sut.CreateAsync(request);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Time-based pricing requires StartTime and EndTime*");
    }

    [Fact]
    public async Task GetByIdAsync_WhenRuleDoesNotExist_ShouldReturnNull()
    {
        var id = Guid.NewGuid();

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingRule?)null);

        var result = await _sut.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenRuleDoesNotExist_ShouldReturnFalse()
    {
        var id = Guid.NewGuid();

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingRule?)null);

        var result = await _sut.DeleteAsync(id);

        result.Should().BeFalse();

        _pricingRuleRepositoryMock.Verify(
            x => x.DeleteAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetActiveAsync_WhenRuleExists_ShouldUpdateActiveStatus()
    {
        var id = Guid.NewGuid();

        var pricingRule = new PricingRule
        {
            Id = id,
            ParkingId = Guid.NewGuid(),
            Name = "Standard Hourly Rate",
            PricingType = "Hourly",
            PricePerHour = 15m,
            IsActive = true
        };

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingRule);

        _pricingRuleRepositoryMock
            .Setup(x => x.UpdateAsync(
                It.IsAny<PricingRule>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.SetActiveAsync(id, false);

        result.Should().BeTrue();
        pricingRule.IsActive.Should().BeFalse();

        _pricingRuleRepositoryMock.Verify(
            x => x.UpdateAsync(
                pricingRule,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetActiveAsync_WhenRuleDoesNotExist_ShouldReturnFalse()
    {
        var id = Guid.NewGuid();

        _pricingRuleRepositoryMock
            .Setup(x => x.GetByIdAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingRule?)null);

        var result = await _sut.SetActiveAsync(id, false);

        result.Should().BeFalse();
    }
}

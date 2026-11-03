using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Parky.Application.Configuration;
using Parky.Application.DTOs.Auth;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Identity;
using Parky.Infrastructure.Services;
using Xunit;

namespace Parky.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var identityOptions = Options.Create(new IdentityOptions());

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();

        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            identityOptions,
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>());

        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            identityOptions,
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<ApplicationUser>>());

        var jwtSettings = new JwtSettings
        {
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        _sut = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtTokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object,
            Options.Create(jwtSettings),
            TimeProvider.System
        );
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldThrowInvalidOperationException()
    {
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Existing User",
            Email = "existing@example.com",
            UserName = "existing@example.com",
            IsActive = true
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        var request = new RegisterRequest
        {
            FullName = "New User",
            Email = "existing@example.com",
            Password = "Password123"
        };

        var act = async () => await _sut.RegisterAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*account with this email already exists*");

        _userManagerMock.Verify(
            x => x.CreateAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserDoesNotExist_ShouldThrowInvalidOperationException()
    {
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var request = new LoginRequest
        {
            Email = "missing@example.com",
            Password = "Password123"
        };

        var act = async () => await _sut.LoginAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid email or password*");

        _signInManagerMock.Verify(
            x => x.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ShouldThrowInvalidOperationException()
    {
        var inactiveUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Inactive User",
            Email = "inactive@example.com",
            UserName = "inactive@example.com",
            IsActive = false
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("inactive@example.com"))
            .ReturnsAsync(inactiveUser);

        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "Password123"
        };

        var act = async () => await _sut.LoginAsync(request);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid email or password*");

        _signInManagerMock.Verify(
            x => x.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ShouldThrowInvalidOperationException()
    {
        var act = async () => await _sut.RefreshTokenAsync(" ");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Refresh token is required*");

        _refreshTokenRepositoryMock.Verify(
            x => x.GetByTokenHashAsync(
                It.IsAny<string>(),
                It.IsAny<System.Threading.CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenTokenDoesNotExist_ShouldThrowInvalidOperationException()
    {
        var act = async () => await _sut.RefreshTokenAsync(
            "unknown-refresh-token");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*refresh token is invalid or expired*");

        _userManagerMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithEmptyToken_ShouldThrowInvalidOperationException()
    {
        var act = async () => await _sut.RevokeRefreshTokenAsync(" ");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Refresh token is required*");

        _refreshTokenRepositoryMock.Verify(
            x => x.GetByTokenHashAsync(
                It.IsAny<string>(),
                It.IsAny<System.Threading.CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenDoesNotExist_ShouldThrowInvalidOperationException()
    {
        var act = async () => await _sut.RevokeRefreshTokenAsync(
            "unknown-refresh-token");

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*refresh token was not found*");

        _refreshTokenRepositoryMock.Verify(
        x => x.RevokeAsync(
        It.IsAny<Guid>(),
        It.IsAny<DateTimeOffset>(),
        It.IsAny<Guid?>(),
        It.IsAny<CancellationToken>()),
         Times.Never);
    }
}


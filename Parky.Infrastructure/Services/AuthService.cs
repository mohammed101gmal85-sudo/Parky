using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Parky.Application.Configuration;
using Parky.Application.DTOs.Auth;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Infrastructure.Identity;

namespace Parky.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private const string DefaultUserRole = "User";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtSettings> jwtOptions,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            throw new InvalidOperationException(
                "An account with this email already exists.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            UserName = email,
            Email = email,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                createResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"User registration failed: {errors}");
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            DefaultUserRole);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Assigning the default role failed: {errors}");
        }

        return await CreateAuthResponseAsync(
            user,
            cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException(
                "Invalid email or password.");
        }

        var signInResult =
            await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Invalid email or password.");
        }

        return await CreateAuthResponseAsync(
            user,
            cancellationToken);
    }


    public async Task<AuthResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                "Refresh token is required.");
        }

        var tokenHash = HashToken(refreshToken);

        var storedToken =
            await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        var now = _timeProvider.GetUtcNow();

        if (storedToken is null ||
            storedToken.RevokedAt.HasValue ||
            storedToken.ExpiresAt <= now)
        {
            throw new InvalidOperationException(
                "The refresh token is invalid or expired.");
        }

        var user = await _userManager.FindByIdAsync(
            storedToken.UserId.ToString());

        if (user is null || !user.IsActive)
        {
            throw new InvalidOperationException(
                "The associated user is unavailable.");
        }

        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenId = Guid.NewGuid();
        var newRefreshTokenExpiresAt = now.AddDays(
            _jwtSettings.RefreshTokenExpirationDays);

        var replacement = new RefreshTokenRecord
        {
            Id = newRefreshTokenId,
            UserId = user.Id,
            TokenHash = HashToken(newRefreshToken),
            CreatedAt = now,
            ExpiresAt = newRefreshTokenExpiresAt
        };

        var replaced = await _refreshTokenRepository.ReplaceAsync(
            storedToken.Id,
            now,
            replacement,
            cancellationToken);

        if (!replaced)
        {
            throw new InvalidOperationException(
                "The refresh token is invalid, expired, or already used.");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var tokenUser = new TokenUserInfo
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToArray()
        };

        var accessToken = _jwtTokenService.GenerateAccessToken(tokenUser);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = now.AddMinutes(
                _jwtSettings.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAt = newRefreshTokenExpiresAt
        };
    }


    public async Task RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException(
                "Refresh token is required.");
        }

        var tokenHash = HashToken(refreshToken);

        var storedToken =
            await _refreshTokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (storedToken is null)
        {
            throw new InvalidOperationException(
                "The refresh token was not found.");
        }

        if (storedToken.RevokedAt.HasValue)
        {
            return;
        }

        await _refreshTokenRepository.RevokeAsync(
            storedToken.Id,
            _timeProvider.GetUtcNow(),
            cancellationToken: cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var accessTokenExpiresAt = now.AddMinutes(
            _jwtSettings.AccessTokenExpirationMinutes);

        var refreshTokenExpiresAt = now.AddDays(
            _jwtSettings.RefreshTokenExpirationDays);

        var roles = await _userManager.GetRolesAsync(user);

        var tokenUser = new TokenUserInfo
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            //Roles = roles
            Roles = roles.ToArray()
        };

        var accessToken =
            _jwtTokenService.GenerateAccessToken(tokenUser);

        var refreshToken = GenerateRefreshToken();

        var refreshTokenRecord = new RefreshTokenRecord
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshTokenExpiresAt
        };

        await _refreshTokenRepository.AddAsync(
            refreshTokenRecord,
            cancellationToken);

        await _refreshTokenRepository.SaveChangesAsync(
            cancellationToken);

        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshTokenExpiresAt = refreshTokenExpiresAt
        };
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);

        return Convert.ToHexString(hashBytes);
    }
}

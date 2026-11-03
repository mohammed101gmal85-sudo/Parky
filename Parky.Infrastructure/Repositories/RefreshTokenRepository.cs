using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parky.Application.DTOs.Auth;
using Parky.Application.Interfaces.Repositories;
using Parky.Infrastructure.Identity;
using Parky.Infrastructure.Persistence;
using System.Data;

namespace Parky.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ParkyDbContext _context;

    public RefreshTokenRepository(ParkyDbContext context)
    {
        _context = context;
    }

    public async Task<RefreshTokenRecord?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .AsNoTracking()
            .Where(x => x.TokenHash == tokenHash)
            .Select(x => new RefreshTokenRecord
            {
                Id = x.Id,
                UserId = x.UserId,
                TokenHash = x.TokenHash,
                ExpiresAt = x.ExpiresAt,
                CreatedAt = x.CreatedAt,
                RevokedAt = x.RevokedAt,
                ReplacedByTokenId = x.ReplacedByTokenId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return token;
    }

    public async Task AddAsync(
        RefreshTokenRecord refreshToken,
        CancellationToken cancellationToken = default)
    {
        var entity = new RefreshToken
        {
            Id = refreshToken.Id == Guid.Empty
                ? Guid.NewGuid()
                : refreshToken.Id,
            UserId = refreshToken.UserId,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedAt = refreshToken.CreatedAt
        };

        await _context.RefreshTokens.AddAsync(
            entity,
            cancellationToken);
    }

    public async Task RevokeAsync(
        Guid tokenId,
        DateTimeOffset revokedAt,
        Guid? replacedByTokenId = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.Id == tokenId,
                cancellationToken);

        if (token is null)
        {
            throw new InvalidOperationException(
                "The refresh token was not found.");
        }

        if (token.RevokedAt.HasValue)
        {
            throw new InvalidOperationException(
                "The refresh token has already been revoked.");
        }

        token.RevokedAt = revokedAt;
        token.ReplacedByTokenId = replacedByTokenId;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ReplaceAsync(
        Guid currentTokenId,
        DateTimeOffset now,
        RefreshTokenRecord replacement,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var currentToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(
                x => x.Id == currentTokenId,
                cancellationToken);

        if (currentToken is null ||
            currentToken.RevokedAt.HasValue ||
            currentToken.ExpiresAt <= now)
        {
            return false;
        }

        var replacementEntity = new RefreshToken
        {
            Id = replacement.Id,
            UserId = replacement.UserId,
            TokenHash = replacement.TokenHash,
            CreatedAt = replacement.CreatedAt,
            ExpiresAt = replacement.ExpiresAt
        };

        currentToken.RevokedAt = now;
        currentToken.ReplacedByTokenId = replacementEntity.Id;

        await _context.RefreshTokens.AddAsync(
            replacementEntity,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

}

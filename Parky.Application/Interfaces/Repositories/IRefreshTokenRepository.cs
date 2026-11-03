using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.DTOs.Auth;

namespace Parky.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshTokenRecord?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshTokenRecord refreshToken,
        CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(
        Guid currentTokenId,
        DateTimeOffset now,
        RefreshTokenRecord replacement,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid tokenId,
        DateTimeOffset revokedAt,
        Guid? replacedByTokenId = null,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}


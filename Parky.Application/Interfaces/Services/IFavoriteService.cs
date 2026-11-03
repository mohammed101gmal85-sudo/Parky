using Parky.Application.Common;
using Parky.Application.DTOs.Favorite;

namespace Parky.Application.Interfaces.Services;

public interface IFavoriteService
{
    Task<FavoriteResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FavoriteResponse>> GetAllAsync(
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FavoriteResponse>> GetMyFavoritesAsync(
        Guid userId,
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<FavoriteResponse> AddAsync(
        CreateFavoriteRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid parkingId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsFavoriteAsync(
        Guid parkingId,
        Guid userId,
        CancellationToken cancellationToken = default);
}
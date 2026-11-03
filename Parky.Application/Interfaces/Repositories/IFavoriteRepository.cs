using Parky.Application.Common;
using Parky.Application.DTOs.Favorite;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IFavoriteRepository
{
    Task<Favorite?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Favorite>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<Favorite>> GetPagedAsync(
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Favorite>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Favorite>> GetPagedByUserIdAsync(
        Guid userId,
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Favorite>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<Favorite?> GetByUserAndParkingAsync(
        Guid userId,
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid userId,
        Guid parkingId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Favorite favorite,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Favorite favorite,
        CancellationToken cancellationToken = default);
}
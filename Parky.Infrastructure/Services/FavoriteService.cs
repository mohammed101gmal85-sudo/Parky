using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.Favorite;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IParkingRepository _parkingRepository;
    private readonly IMapper _mapper;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        IParkingRepository parkingRepository,
        IMapper mapper)
    {
        _favoriteRepository = favoriteRepository;
        _parkingRepository = parkingRepository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Get Favorite By Id
    // ----------------------------------------------------

    public async Task<FavoriteResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var favorite = await _favoriteRepository.GetByIdAsync(
            id,
            cancellationToken);

        return favorite is null
            ? null
            : MapToResponse(favorite);
    }

    // ----------------------------------------------------
    // Get All Favorites
    // ----------------------------------------------------

    public async Task<PagedResult<FavoriteResponse>> GetAllAsync(
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _favoriteRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get My Favorites
    // ----------------------------------------------------

    public async Task<PagedResult<FavoriteResponse>> GetMyFavoritesAsync(
        Guid userId,
        FavoriteQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _favoriteRepository.GetPagedByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Add Favorite
    // ----------------------------------------------------

    public async Task<FavoriteResponse> AddAsync(
        CreateFavoriteRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidUserId(userId);

        if (request.ParkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        var parkingExists = await _parkingRepository.ExistsAsync(
            request.ParkingId,
            cancellationToken);

        if (!parkingExists)
        {
            throw new KeyNotFoundException(
                "The specified parking was not found.");
        }

        var alreadyFavorite = await _favoriteRepository.ExistsAsync(
            userId,
            request.ParkingId,
            cancellationToken);

        if (alreadyFavorite)
        {
            throw new InvalidOperationException(
                "This parking is already in your favorites.");
        }

        var favorite = new Favorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ParkingId = request.ParkingId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _favoriteRepository.AddAsync(
            favorite,
            cancellationToken);

        return MapToResponse(favorite);
    }

    // ----------------------------------------------------
    // Remove Favorite
    // ----------------------------------------------------

    public async Task RemoveAsync(
        Guid parkingId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        EnsureValidUserId(userId);

        var favorite = await _favoriteRepository.GetByUserAndParkingAsync(
            userId,
            parkingId,
            cancellationToken);

        if (favorite is null)
        {
            throw new KeyNotFoundException(
                "This parking was not found in your favorites.");
        }

        await _favoriteRepository.DeleteAsync(
            favorite,
            cancellationToken);
    }

    // ----------------------------------------------------
    // Check If Favorite
    // ----------------------------------------------------

    public async Task<bool> IsFavoriteAsync(
        Guid parkingId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.");
        }

        EnsureValidUserId(userId);

        return await _favoriteRepository.ExistsAsync(
            userId,
            parkingId,
            cancellationToken);
    }

    // ----------------------------------------------------
    // Validation Helpers
    // ----------------------------------------------------

    private static void EnsureValidUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is required.");
        }
    }

    private static void ValidateQueryParameters(
        FavoriteQueryParameters parameters)
    {
        if (parameters.UserId.HasValue &&
            parameters.UserId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID is invalid.");
        }

        if (parameters.ParkingId.HasValue &&
            parameters.ParkingId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is invalid.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private PagedResult<FavoriteResponse> MapToPagedResponse(
        PagedResult<Favorite> result)
    {
        return new PagedResult<FavoriteResponse>
        {
            Items = _mapper.Map<List<FavoriteResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private FavoriteResponse MapToResponse(Favorite favorite)
    {
        return _mapper.Map<FavoriteResponse>(favorite);
    }
}
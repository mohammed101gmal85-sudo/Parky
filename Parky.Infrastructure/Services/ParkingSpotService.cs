using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpot;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;

namespace Parky.Infrastructure.Services;

public sealed class ParkingSpotService : IParkingSpotService
{
    private readonly IParkingSpotRepository _repository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    public ParkingSpotService(
    IParkingSpotRepository repository,
    IMapper mapper,
    ICacheService cacheService,
    IOptions<CacheOptions> cacheOptions)
    {
        _repository = repository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }

    // ----------------------------------------------------
    // Create Parking Spot
    // ----------------------------------------------------

    public async Task<ParkingSpotResponse> CreateAsync(
        CreateParkingSpotRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.SpotNumber,
            request.XCoordinate,
            request.YCoordinate,
            request.Width,
            request.Height);

        if (!await _repository.ParkingFloorExistsAsync(
                request.ParkingFloorId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Parking floor was not found.");
        }

        if (!await _repository.ParkingFloorBelongsToOwnerAsync(
                request.ParkingFloorId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking floor.");
        }

        if (await _repository.SpotNumberExistsAsync(
                request.ParkingFloorId,
                request.SpotNumber.Trim(),
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot with this number already exists on this floor.");
        }

        await ValidateSpotTypeAsync(
            request.ParkingSpotTypeId,
            cancellationToken);

        var parkingSpot = new ParkingSpot
        {
            ParkingFloorId = request.ParkingFloorId,
            SpotNumber = request.SpotNumber.Trim(),
            Description = NormalizeOptionalText(request.Description),
            XCoordinate = request.XCoordinate,
            YCoordinate = request.YCoordinate,
            Width = request.Width,
            Height = request.Height,
            ParkingSpotTypeId = request.ParkingSpotTypeId,
            IsAvailable = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(
            parkingSpot,
            cancellationToken);

        await InvalidateParkingFloorCacheAsync(
          parkingSpot.ParkingFloorId,
          cancellationToken);

        return MapToResponse(parkingSpot);
    }

    // ----------------------------------------------------
    // Get Parking Spot By Id
    // ----------------------------------------------------

    public async Task<ParkingSpotResponse?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ParkingSpotById(id);

        var cachedSpot =
            await _cacheService.GetAsync<ParkingSpotResponse>(
                cacheKey,
                cancellationToken);

        if (cachedSpot is not null)
        {
            return cachedSpot;
        }

        var parkingSpot = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpot is null)
        {
            return null;
        }

        var response = MapToResponse(parkingSpot);

        await _cacheService.SetAsync(
            cacheKey,
            response,
            _cacheOptions.DefaultExpiration,
            cancellationToken);

        return response;
    }

    // ----------------------------------------------------
    // Get All Parking Spots
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingSpotResponse>> GetAllAsync(
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _repository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Parking Spots By Parking Floor Id
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingSpotResponse>>
        GetByParkingFloorIdAsync(
            Guid parkingFloorId,
            ParkingSpotQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        if (!await _repository.ParkingFloorExistsAsync(
                parkingFloorId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Parking floor was not found.");
        }

        var result = await _repository.GetPagedByParkingFloorIdAsync(
            parkingFloorId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Available Parking Spots By Parking Floor Id
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingSpotResponse>>
    GetAvailableByParkingFloorIdAsync(
        Guid parkingFloorId,
        ParkingSpotQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (!await _repository.ParkingFloorExistsAsync(
                parkingFloorId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Parking floor was not found.");
        }

        var queryKey = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(parameters))));

        var cacheKey = CacheKeys.ParkingAvailability(
            parkingFloorId,
            queryKey);

        var cachedResult =
            await _cacheService.GetAsync<PagedResult<ParkingSpotResponse>>(
                cacheKey,
                cancellationToken);

        if (cachedResult is not null)
        {
            return cachedResult;
        }

        var result =
            await _repository.GetPagedAvailableByParkingFloorIdAsync(
                parkingFloorId,
                parameters,
                cancellationToken);

        var response = MapToPagedResponse(result);

        await _cacheService.SetAsync(
            cacheKey,
            response,
            _cacheOptions.ParkingAvailabilityExpiration,
            cancellationToken);

        return response;
    }

    // ----------------------------------------------------
    // Update Parking Spot
    // ----------------------------------------------------

    public async Task<ParkingSpotResponse?> UpdateAsync(
        Guid id,
        UpdateParkingSpotRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(
            request.SpotNumber,
            request.XCoordinate,
            request.YCoordinate,
            request.Width,
            request.Height);

        var parkingSpot = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        var oldParkingFloorId = parkingSpot.ParkingFloorId;

        if (parkingSpot is null)
        {
            return null;
        }

        if (!await _repository.ParkingFloorBelongsToOwnerAsync(
                parkingSpot.ParkingFloorId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking spot.");
        }

        if (!await _repository.ParkingFloorExistsAsync(
                request.ParkingFloorId,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Target parking floor was not found.");
        }

        if (!await _repository.ParkingFloorBelongsToOwnerAsync(
                request.ParkingFloorId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to move the spot to this parking floor.");
        }

        if (await _repository.SpotNumberExistsAsync(
                request.ParkingFloorId,
                request.SpotNumber.Trim(),
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot with this number already exists on the target floor.");
        }

        await ValidateSpotTypeAsync(
            request.ParkingSpotTypeId,
            cancellationToken);

        parkingSpot.ParkingFloorId = request.ParkingFloorId;
        parkingSpot.SpotNumber = request.SpotNumber.Trim();
        parkingSpot.Description = NormalizeOptionalText(request.Description);
        parkingSpot.XCoordinate = request.XCoordinate;
        parkingSpot.YCoordinate = request.YCoordinate;
        parkingSpot.Width = request.Width;
        parkingSpot.Height = request.Height;
        parkingSpot.ParkingSpotTypeId = request.ParkingSpotTypeId;
        parkingSpot.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            parkingSpot,
            cancellationToken);

        await _cacheService.RemoveAsync(
          CacheKeys.ParkingSpotById(id),
           cancellationToken);

        await InvalidateParkingFloorCacheAsync(
            oldParkingFloorId,
            cancellationToken);

        if (oldParkingFloorId != parkingSpot.ParkingFloorId)
        {
            await InvalidateParkingFloorCacheAsync(
                parkingSpot.ParkingFloorId,
                cancellationToken);
        }

        return MapToResponse(parkingSpot);
    }

    // ----------------------------------------------------
    // Delete Parking Spot
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var parkingSpot = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpot is null)
        {
            return false;
        }

        if (!await _repository.ParkingFloorBelongsToOwnerAsync(
                parkingSpot.ParkingFloorId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking spot.");
        }

        parkingSpot.IsActive = false;
        parkingSpot.IsAvailable = false;
        parkingSpot.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            parkingSpot,
            cancellationToken);

        await _cacheService.RemoveAsync(
           CacheKeys.ParkingSpotById(id),
           cancellationToken);

        await InvalidateParkingFloorCacheAsync(
            parkingSpot.ParkingFloorId,
            cancellationToken);


        return true;
    }

    // ----------------------------------------------------
    // Update Parking Spot Status
    // ----------------------------------------------------

    public async Task<ParkingSpotResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingSpotStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsAvailable.HasValue &&
            !request.IsActive.HasValue)
        {
            throw new ArgumentException(
                "At least one status field must be provided.");
        }

        var parkingSpot = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpot is null)
        {
            return null;
        }

        if (!await _repository.ParkingFloorBelongsToOwnerAsync(
                parkingSpot.ParkingFloorId,
                ownerId,
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to manage this parking spot.");
        }

        if (request.IsActive.HasValue)
        {
            parkingSpot.IsActive = request.IsActive.Value;

            if (!parkingSpot.IsActive)
            {
                parkingSpot.IsAvailable = false;
            }
        }

        if (request.IsAvailable.HasValue)
        {
            if (request.IsAvailable.Value && !parkingSpot.IsActive)
            {
                throw new InvalidOperationException(
                    "An inactive parking spot cannot be marked as available.");
            }

            parkingSpot.IsAvailable = request.IsAvailable.Value;
        }

        parkingSpot.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            parkingSpot,
            cancellationToken);

        await _cacheService.RemoveAsync(
          CacheKeys.ParkingSpotById(id),
           cancellationToken);

        await InvalidateParkingFloorCacheAsync(
            parkingSpot.ParkingFloorId,
            cancellationToken);

        return MapToResponse(parkingSpot);
    }

    // ----------------------------------------------------
    // Validate Parking Spot Type
    // ----------------------------------------------------

    private async Task ValidateSpotTypeAsync(
        Guid? parkingSpotTypeId,
        CancellationToken cancellationToken)
    {
        if (!parkingSpotTypeId.HasValue)
        {
            return;
        }

        if (!await _repository.ParkingSpotTypeExistsAsync(
                parkingSpotTypeId.Value,
                cancellationToken))
        {
            throw new KeyNotFoundException(
                "Parking spot type was not found.");
        }
    }

    // ----------------------------------------------------
    // Validate Request
    // ----------------------------------------------------

    private static void ValidateRequest(
        string spotNumber,
        decimal? xCoordinate,
        decimal? yCoordinate,
        decimal? width,
        decimal? height)
    {
        if (string.IsNullOrWhiteSpace(spotNumber))
        {
            throw new ArgumentException(
                "SpotNumber is required.");
        }

        if (xCoordinate.HasValue != yCoordinate.HasValue)
        {
            throw new ArgumentException(
                "XCoordinate and YCoordinate must be provided together.");
        }

        if (width.HasValue != height.HasValue)
        {
            throw new ArgumentException(
                "Width and Height must be provided together.");
        }

        if (width.HasValue && width.Value <= 0)
        {
            throw new ArgumentException(
                "Width must be greater than zero.");
        }

        if (height.HasValue && height.Value <= 0)
        {
            throw new ArgumentException(
                "Height must be greater than zero.");
        }
    }

    // ----------------------------------------------------
    // Normalize Optional Text
    // ----------------------------------------------------

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingSpotResponse MapToResponse(ParkingSpot parkingSpot)
    {
        return _mapper.Map<ParkingSpotResponse>(parkingSpot);
    }

    private PagedResult<ParkingSpotResponse> MapToPagedResponse(
        PagedResult<ParkingSpot> result)
    {
        return new PagedResult<ParkingSpotResponse>
        {
            Items = _mapper.Map<List<ParkingSpotResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private async Task InvalidateParkingFloorCacheAsync(
    Guid parkingFloorId,
    CancellationToken cancellationToken)
    {
        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.ParkingAvailabilityByFloorPrefix(parkingFloorId),
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.ParkingFloorSpotsPrefix(parkingFloorId),
            cancellationToken);
    }
}

using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.Parking;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Microsoft.Extensions.Options;
using Parky.Infrastructure.Caching;
using Parky.Infrastructure.Settings;

namespace Parky.Infrastructure.Services;

public sealed class ParkingService : IParkingService
{
    private readonly IParkingRepository _parkingRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly CacheOptions _cacheOptions;
    public ParkingService(
    IParkingRepository parkingRepository,
    IMapper mapper,
    ICacheService cacheService,
    IOptions<CacheOptions> cacheOptions)
    {
        _parkingRepository = parkingRepository;
        _mapper = mapper;
        _cacheService = cacheService;
        _cacheOptions = cacheOptions.Value;
    }
    public async Task<ParkingResponse> CreateAsync(
        CreateParkingRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var parking = new Parking
        {
            OwnerId = ownerId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Governorate = request.Governorate.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _parkingRepository.AddAsync(
            parking,
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
           CacheKeys.ParkingListPrefix(),
           cancellationToken);

        return MapToResponse(parking);
    }

    public async Task<ParkingResponse?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.ParkingById(id);

        var cachedParking =
            await _cacheService.GetAsync<ParkingResponse>(
                cacheKey,
                cancellationToken);

        if (cachedParking is not null)
        {
            return cachedParking;
        }

        var parking = await _parkingRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (parking is null || !parking.IsActive)
        {
            return null;
        }

        var response = MapToResponse(parking);

        await _cacheService.SetAsync(
            cacheKey,
            response,
            _cacheOptions.ParkingExpiration,
            cancellationToken);

        return response;
    }

    public async Task<PagedResult<ParkingResponse>> GetAllAsync(
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _parkingRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<PagedResult<ParkingResponse>> GetMyParkingsAsync(
        Guid ownerId,
        ParkingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var result = await _parkingRepository.GetPagedByOwnerIdAsync(
            ownerId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    public async Task<ParkingResponse?> UpdateAsync(
        Guid id,
        Guid ownerId,
        UpdateParkingRequest request,
        CancellationToken cancellationToken = default)
    {
        var parking = await _parkingRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (parking is null || parking.OwnerId != ownerId)
        {
            return null;
        }

        parking.Name = request.Name.Trim();
        parking.Description = request.Description.Trim();
        parking.Governorate = request.Governorate.Trim();
        parking.City = request.City.Trim();
        parking.Address = request.Address.Trim();
        parking.Latitude = request.Latitude;
        parking.Longitude = request.Longitude;
        parking.UpdatedAt = DateTimeOffset.UtcNow;

        await _parkingRepository.UpdateAsync(
            parking,
            cancellationToken);

        await InvalidateParkingCacheAsync(
            parking.Id,
            cancellationToken);

        return MapToResponse(parking);
    }

    public async Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var parking = await _parkingRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (parking is null ||
            parking.OwnerId != ownerId ||
            !parking.IsActive)
        {
            return false;
        }

        parking.IsActive = false;
        parking.UpdatedAt = DateTimeOffset.UtcNow;

        await _parkingRepository.UpdateAsync(
            parking,
            cancellationToken);

        await InvalidateParkingCacheAsync(
           parking.Id,
           cancellationToken);

        return true;
    }

    private PagedResult<ParkingResponse> MapToPagedResponse(
        PagedResult<Parking> result)
    {
        return new PagedResult<ParkingResponse>
        {
            Items = _mapper.Map<List<ParkingResponse>>(result.Items),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private ParkingResponse MapToResponse(Parking parking)
    {
        return _mapper.Map<ParkingResponse>(parking);
    }

    private async Task InvalidateParkingCacheAsync(
    Guid parkingId,
    CancellationToken cancellationToken)
    {
        await _cacheService.RemoveAsync(
            CacheKeys.ParkingById(parkingId),
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.ParkingListPrefix(),
            cancellationToken);

        await _cacheService.RemoveByPrefixAsync(
            CacheKeys.ParkingRelatedPrefix(parkingId),
            cancellationToken);
    }
}

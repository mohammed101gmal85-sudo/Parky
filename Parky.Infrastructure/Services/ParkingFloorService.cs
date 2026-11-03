using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingFloor;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingFloorService : IParkingFloorService
{
    private readonly IParkingFloorRepository _repository;
    private readonly IMapper _mapper;

    public ParkingFloorService(
        IParkingFloorRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Parking Floor
    // ----------------------------------------------------

    public async Task<ParkingFloorResponse> CreateAsync(
        CreateParkingFloorRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            request.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "The parking does not exist, is inactive, or does not belong to you.");
        }

        var floorNumberExists = await _repository.FloorNumberExistsAsync(
            request.ParkingId,
            request.FloorNumber,
            cancellationToken: cancellationToken);

        if (floorNumberExists)
        {
            throw new InvalidOperationException(
                "A floor with this number already exists in this parking.");
        }

        var floor = new ParkingFloor
        {
            ParkingId = request.ParkingId,
            Name = request.Name.Trim(),
            FloorNumber = request.FloorNumber,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(
            floor,
            cancellationToken);

        return MapToResponse(floor);
    }

    // ----------------------------------------------------
    // Get Parking Floor By Id
    // ----------------------------------------------------

    public async Task<ParkingFloorResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var floor = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (floor is null || !floor.IsActive)
        {
            return null;
        }

        return MapToResponse(floor);
    }

    // ----------------------------------------------------
    // Get All Parking Floors
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingFloorResponse>> GetAllAsync(
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _repository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Active Floors Internal
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingFloor>> GetActiveFloorsInternalAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return Array.Empty<ParkingFloor>();
        }

        return await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);
    }

    // ----------------------------------------------------
    // Get Parking Floors By Parking Id
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingFloorResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingFloorQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is invalid.");
        }

        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return new PagedResult<ParkingFloorResponse>
            {
                Items = Array.Empty<ParkingFloorResponse>(),
                PageNumber = parameters.PageNumber,
                PageSize = parameters.PageSize,
                TotalCount = 0
            };
        }

        var result = await _repository.GetPagedByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Update Parking Floor
    // ----------------------------------------------------

    public async Task<ParkingFloorResponse?> UpdateAsync(
        Guid id,
        UpdateParkingFloorRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var floor = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (floor is null || !floor.IsActive)
        {
            return null;
        }

        var ownsCurrentParking =
            await _repository.ParkingBelongsToOwnerAsync(
                floor.ParkingId,
                ownerId,
                cancellationToken);

        if (!ownsCurrentParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this floor.");
        }

        var ownsTargetParking =
            await _repository.ParkingBelongsToOwnerAsync(
                request.ParkingId,
                ownerId,
                cancellationToken);

        if (!ownsTargetParking)
        {
            throw new InvalidOperationException(
                "The target parking does not exist, is inactive, or does not belong to you.");
        }

        var floorNumberExists = await _repository.FloorNumberExistsAsync(
            request.ParkingId,
            request.FloorNumber,
            id,
            cancellationToken);

        if (floorNumberExists)
        {
            throw new InvalidOperationException(
                "A floor with this number already exists in this parking.");
        }

        floor.ParkingId = request.ParkingId;
        floor.Name = request.Name.Trim();
        floor.FloorNumber = request.FloorNumber;
        floor.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            floor,
            cancellationToken);

        return MapToResponse(floor);
    }

    // ----------------------------------------------------
    // Deactivate Parking Floor
    // ----------------------------------------------------

    public async Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var floor = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (floor is null || !floor.IsActive)
        {
            return false;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            floor.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this floor.");
        }

        floor.IsActive = false;
        floor.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            floor,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Update Parking Floor Status
    // ----------------------------------------------------

    public async Task<ParkingFloorResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingFloorStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var floor = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (floor is null)
        {
            return null;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            floor.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this floor.");
        }

        if (request.IsActive)
        {
            var parkingIsActive = await _repository.ParkingExistsAsync(
                floor.ParkingId,
                cancellationToken);

            if (!parkingIsActive)
            {
                throw new InvalidOperationException(
                    "Cannot activate a floor under a missing or inactive parking.");
            }
        }

        if (request.IsActive)
        {
            var duplicateNumber = await _repository.FloorNumberExistsAsync(
                floor.ParkingId,
                floor.FloorNumber,
                floor.Id,
                cancellationToken);

            if (duplicateNumber)
            {
                throw new InvalidOperationException(
                    "Another floor with this number already exists in this parking.");
            }
        }

        floor.IsActive = request.IsActive;
        floor.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            floor,
            cancellationToken);

        return MapToResponse(floor);
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        ParkingFloorQueryParameters parameters)
    {
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

        if (parameters.UpdatedFrom.HasValue &&
            parameters.UpdatedTo.HasValue &&
            parameters.UpdatedFrom.Value > parameters.UpdatedTo.Value)
        {
            throw new ArgumentException(
                "UpdatedFrom cannot be later than UpdatedTo.");
        }
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingFloorResponse MapToResponse(ParkingFloor floor)
    {
        return _mapper.Map<ParkingFloorResponse>(floor);
    }

    private PagedResult<ParkingFloorResponse> MapToPagedResponse(
        PagedResult<ParkingFloor> result)
    {
        return new PagedResult<ParkingFloorResponse>
        {
            Items = _mapper.Map<List<ParkingFloorResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
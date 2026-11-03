using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingSpotType;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingSpotTypeService : IParkingSpotTypeService
{
    private readonly IParkingSpotTypeRepository _repository;
    private readonly IMapper _mapper;

    public ParkingSpotTypeService(
        IParkingSpotTypeRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Parking Spot Type
    // ----------------------------------------------------

    public async Task<ParkingSpotTypeResponse> CreateAsync(
        CreateParkingSpotTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(
            request.Name,
            request.Code,
            request.AdditionalHourlyRate);

        var name = request.Name.Trim();
        var code = NormalizeCode(request.Code);

        if (await _repository.NameExistsAsync(
                name,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot type with this name already exists.");
        }

        if (await _repository.CodeExistsAsync(
                code,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot type with this code already exists.");
        }

        var parkingSpotType = new ParkingSpotType
        {
            Name = name,
            Code = code,
            Description = NormalizeOptionalText(request.Description),
            AdditionalHourlyRate = request.AdditionalHourlyRate,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(
            parkingSpotType,
            cancellationToken);

        return MapToResponse(parkingSpotType);
    }

    // ----------------------------------------------------
    // Get Parking Spot Type By Id
    // ----------------------------------------------------

    public async Task<ParkingSpotTypeResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var parkingSpotType = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        return parkingSpotType is null
            ? null
            : MapToResponse(parkingSpotType);
    }

    // ----------------------------------------------------
    // Get All Parking Spot Types
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingSpotTypeResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var parkingSpotTypes = await _repository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<ParkingSpotTypeResponse>>(
            parkingSpotTypes);
    }

    // ----------------------------------------------------
    // Get All Parking Spot Types With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingSpotTypeResponse>> GetAllAsync(
        ParkingSpotTypeQueryParameters parameters,
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
    // Get Active Parking Spot Types
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingSpotTypeResponse>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var parkingSpotTypes = await _repository.GetActiveAsync(
            cancellationToken);

        return _mapper.Map<List<ParkingSpotTypeResponse>>(
            parkingSpotTypes);
    }

    // ----------------------------------------------------
    // Update Parking Spot Type
    // ----------------------------------------------------

    public async Task<ParkingSpotTypeResponse?> UpdateAsync(
        Guid id,
        UpdateParkingSpotTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(
            request.Name,
            request.Code,
            request.AdditionalHourlyRate);

        var parkingSpotType = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpotType is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var code = NormalizeCode(request.Code);

        if (await _repository.NameExistsAsync(
                name,
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot type with this name already exists.");
        }

        if (await _repository.CodeExistsAsync(
                code,
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A parking spot type with this code already exists.");
        }

        parkingSpotType.Name = name;
        parkingSpotType.Code = code;
        parkingSpotType.Description =
            NormalizeOptionalText(request.Description);
        parkingSpotType.AdditionalHourlyRate =
            request.AdditionalHourlyRate;

        await _repository.UpdateAsync(
            parkingSpotType,
            cancellationToken);

        return MapToResponse(parkingSpotType);
    }

    // ----------------------------------------------------
    // Update Parking Spot Type Status
    // ----------------------------------------------------

    public async Task<ParkingSpotTypeResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingSpotTypeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parkingSpotType = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpotType is null)
        {
            return null;
        }

        parkingSpotType.IsActive = request.IsActive;

        await _repository.UpdateAsync(
            parkingSpotType,
            cancellationToken);

        return MapToResponse(parkingSpotType);
    }

    // ----------------------------------------------------
    // Delete Parking Spot Type
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var parkingSpotType = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (parkingSpotType is null)
        {
            return false;
        }

        var hasAssignedSpots =
            await _repository.HasAssignedParkingSpotsAsync(
                id,
                cancellationToken);

        if (hasAssignedSpots)
        {
            parkingSpotType.IsActive = false;

            await _repository.UpdateAsync(
                parkingSpotType,
                cancellationToken);

            return true;
        }

        await _repository.DeleteAsync(
            parkingSpotType,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        ParkingSpotTypeQueryParameters parameters)
    {
        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.MinAdditionalHourlyRate.HasValue &&
            parameters.MinAdditionalHourlyRate.Value < 0)
        {
            throw new ArgumentException(
                "MinAdditionalHourlyRate cannot be negative.");
        }

        if (parameters.MaxAdditionalHourlyRate.HasValue &&
            parameters.MaxAdditionalHourlyRate.Value < 0)
        {
            throw new ArgumentException(
                "MaxAdditionalHourlyRate cannot be negative.");
        }

        if (parameters.MinAdditionalHourlyRate.HasValue &&
            parameters.MaxAdditionalHourlyRate.HasValue &&
            parameters.MinAdditionalHourlyRate.Value >
            parameters.MaxAdditionalHourlyRate.Value)
        {
            throw new ArgumentException(
                "MinAdditionalHourlyRate cannot be greater than MaxAdditionalHourlyRate.");
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
    // Validate Request
    // ----------------------------------------------------

    private static void ValidateRequest(
        string name,
        string code,
        decimal? additionalHourlyRate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Name is required.");
        }

        if (name.Trim().Length < 2 ||
            name.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Name must contain between 2 and 100 characters.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "Code is required.");
        }

        var normalizedCode = NormalizeCode(code);

        if (normalizedCode.Length < 2 ||
            normalizedCode.Length > 30)
        {
            throw new ArgumentException(
                "Code must contain between 2 and 30 characters.");
        }

        if (normalizedCode.Any(
                character =>
                    !char.IsAsciiLetterOrDigit(character) &&
                    character != '_' &&
                    character != '-'))
        {
            throw new ArgumentException(
                "Code can contain only English letters, numbers, underscores, and hyphens.");
        }

        if (additionalHourlyRate.HasValue &&
            additionalHourlyRate.Value < 0)
        {
            throw new ArgumentException(
                "AdditionalHourlyRate cannot be negative.");
        }
    }

    // ----------------------------------------------------
    // Normalize Code
    // ----------------------------------------------------

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
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

    private ParkingSpotTypeResponse MapToResponse(
        ParkingSpotType parkingSpotType)
    {
        return _mapper.Map<ParkingSpotTypeResponse>(
            parkingSpotType);
    }

    private PagedResult<ParkingSpotTypeResponse> MapToPagedResponse(
        PagedResult<ParkingSpotType> result)
    {
        return new PagedResult<ParkingSpotTypeResponse>
        {
            Items = _mapper.Map<List<ParkingSpotTypeResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
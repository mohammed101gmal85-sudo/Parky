using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingGate;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingGateService : IParkingGateService
{
    private readonly IParkingGateRepository _repository;
    private readonly IMapper _mapper;

    public ParkingGateService(
        IParkingGateRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Parking Gate
    // ----------------------------------------------------

    public async Task<ParkingGateResponse> CreateAsync(
        CreateParkingGateRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        ValidateGateDirection(
            request.IsEntryGate,
            request.IsExitGate);

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            request.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "The parking does not exist, is inactive, or does not belong to you.");
        }

        var normalizedCode = request.GateCode.Trim();

        var codeExists = await _repository.GateCodeExistsAsync(
            normalizedCode,
            cancellationToken: cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException(
                "A parking gate with this code already exists.");
        }

        var gate = new ParkingGate
        {
            ParkingId = request.ParkingId,
            Name = request.Name.Trim(),
            GateCode = normalizedCode,
            GateType = request.GateType.Trim(),
            LocationDescription = NormalizeOptionalText(
                request.LocationDescription),
            IsEntryGate = request.IsEntryGate,
            IsExitGate = request.IsExitGate,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _repository.AddAsync(
            gate,
            cancellationToken);

        return MapToResponse(gate);
    }

    // ----------------------------------------------------
    // Get Parking Gate By Id
    // ----------------------------------------------------

    public async Task<ParkingGateResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var gate = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (gate is null || !gate.IsActive)
        {
            return null;
        }

        return MapToResponse(gate);
    }

    // ----------------------------------------------------
    // Get All Parking Gates
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingGateResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var gates = await _repository.GetAllAsync(
            cancellationToken);

        var activeGates = gates
            .Where(gate => gate.IsActive)
            .ToList();

        return _mapper.Map<List<ParkingGateResponse>>(
            activeGates);
    }

    // ----------------------------------------------------
    // Get All Parking Gates With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingGateResponse>> GetAllAsync(
        ParkingGateQueryParameters parameters,
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
    // Get Parking Gates By Parking Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<ParkingGateResponse>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return Array.Empty<ParkingGateResponse>();
        }

        var gates = await _repository.GetByParkingIdAsync(
            parkingId,
            cancellationToken);

        return _mapper.Map<List<ParkingGateResponse>>(
            gates);
    }

    // ----------------------------------------------------
    // Get Parking Gates By Parking Id With Pagination
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingGateResponse>> GetByParkingIdAsync(
        Guid parkingId,
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parkingId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking ID is required.",
                nameof(parkingId));
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var parkingExists = await _repository.ParkingExistsAsync(
            parkingId,
            cancellationToken);

        if (!parkingExists)
        {
            return CreateEmptyPagedResponse(parameters);
        }

        var result = await _repository.GetPagedByParkingIdAsync(
            parkingId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Update Parking Gate
    // ----------------------------------------------------

    public async Task<ParkingGateResponse?> UpdateAsync(
        Guid id,
        UpdateParkingGateRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        ValidateGateDirection(
            request.IsEntryGate,
            request.IsExitGate);

        var gate = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (gate is null || !gate.IsActive)
        {
            return null;
        }

        var ownsCurrentParking =
            await _repository.ParkingBelongsToOwnerAsync(
                gate.ParkingId,
                ownerId,
                cancellationToken);

        if (!ownsCurrentParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this gate.");
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

        var normalizedCode = request.GateCode.Trim();

        var codeExists = await _repository.GateCodeExistsAsync(
            normalizedCode,
            id,
            cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException(
                "Another parking gate with this code already exists.");
        }

        gate.ParkingId = request.ParkingId;
        gate.Name = request.Name.Trim();
        gate.GateCode = normalizedCode;
        gate.GateType = request.GateType.Trim();
        gate.LocationDescription = NormalizeOptionalText(
            request.LocationDescription);
        gate.IsEntryGate = request.IsEntryGate;
        gate.IsExitGate = request.IsExitGate;
        gate.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            gate,
            cancellationToken);

        return MapToResponse(gate);
    }

    // ----------------------------------------------------
    // Deactivate Parking Gate
    // ----------------------------------------------------

    public async Task<bool> DeactivateAsync(
        Guid id,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidOwnerId(ownerId);

        var gate = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (gate is null || !gate.IsActive)
        {
            return false;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            gate.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this gate.");
        }

        gate.IsActive = false;
        gate.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            gate,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Update Parking Gate Status
    // ----------------------------------------------------

    public async Task<ParkingGateResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingGateStatusRequest request,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidOwnerId(ownerId);

        var gate = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (gate is null)
        {
            return null;
        }

        var ownsParking = await _repository.ParkingBelongsToOwnerAsync(
            gate.ParkingId,
            ownerId,
            cancellationToken);

        if (!ownsParking)
        {
            throw new InvalidOperationException(
                "You do not own the parking associated with this gate.");
        }

        if (request.IsActive)
        {
            var parkingIsActive = await _repository.ParkingExistsAsync(
                gate.ParkingId,
                cancellationToken);

            if (!parkingIsActive)
            {
                throw new InvalidOperationException(
                    "Cannot activate a gate under a missing or inactive parking.");
            }
        }

        gate.IsActive = request.IsActive;
        gate.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(
            gate,
            cancellationToken);

        return MapToResponse(gate);
    }

    // ----------------------------------------------------
    // Validation Helpers
    // ----------------------------------------------------

    private static void EnsureValidOwnerId(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner ID is required.",
                nameof(ownerId));
        }
    }

    private static void ValidateQueryParameters(
        ParkingGateQueryParameters parameters)
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

    private static void ValidateGateDirection(
        bool isEntryGate,
        bool isExitGate)
    {
        if (!isEntryGate && !isExitGate)
        {
            throw new InvalidOperationException(
                "A gate must be configured as an entry gate, an exit gate, or both.");
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingGateResponse MapToResponse(ParkingGate gate)
    {
        return _mapper.Map<ParkingGateResponse>(gate);
    }

    private PagedResult<ParkingGateResponse> MapToPagedResponse(
        PagedResult<ParkingGate> result)
    {
        return new PagedResult<ParkingGateResponse>
        {
            Items = _mapper.Map<List<ParkingGateResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private static PagedResult<ParkingGateResponse> CreateEmptyPagedResponse(
        ParkingGateQueryParameters parameters)
    {
        return new PagedResult<ParkingGateResponse>
        {
            Items = new List<ParkingGateResponse>(),
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = 0
        };
    }
}
using AutoMapper;
using Parky.Application.DTOs.Vehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;
using Parky.Application.Common;

namespace Parky.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public VehicleService(
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Vehicle
    // ----------------------------------------------------

    public async Task<VehicleResponse> CreateAsync(
        CreateVehicleRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A valid user is required.");
        }

        ValidateVehicleData(
            request.PlateNumber,
            request.PlateCode,
            request.Make,
            request.Model,
            request.VehicleType,
            request.ManufacturingYear);

        var plateNumber = request.PlateNumber.Trim();
        var plateCode = NormalizeOptional(request.PlateCode);

        var plateExists = await _vehicleRepository.PlateNumberExistsAsync(
            plateNumber,
            plateCode,
            cancellationToken: cancellationToken);

        if (plateExists)
        {
            throw new InvalidOperationException(
                "A vehicle with this plate number and code already exists.");
        }

        var userVehicles = await _vehicleRepository.GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        var shouldBeDefault =
            request.IsDefault || userVehicles.Count == 0;

        if (shouldBeDefault)
        {
            foreach (var existingVehicle in userVehicles.Where(
                         x => x.IsDefault))
            {
                existingVehicle.IsDefault = false;
                existingVehicle.UpdatedAt = DateTimeOffset.UtcNow;

                await _vehicleRepository.UpdateAsync(
                    existingVehicle,
                    cancellationToken);
            }
        }

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlateNumber = plateNumber,
            PlateCode = plateCode,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            Color = NormalizeOptional(request.Color),
            ManufacturingYear = request.ManufacturingYear,
            VehicleType = request.VehicleType.Trim(),
            IsDefault = shouldBeDefault,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _vehicleRepository.AddAsync(
            vehicle,
            cancellationToken);

        return MapToResponse(vehicle);
    }

    // ----------------------------------------------------
    // Get Vehicle By Id
    // ----------------------------------------------------

    public async Task<VehicleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        return vehicle is null
            ? null
            : MapToResponse(vehicle);
    }

    // ----------------------------------------------------
    // Get All Vehicles
    // ----------------------------------------------------

    public async Task<PagedResult<VehicleResponse>> GetAllAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _vehicleRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get My Vehicles
    // ----------------------------------------------------

    public async Task<PagedResult<VehicleResponse>> GetMyVehiclesAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _vehicleRepository.GetPagedActiveByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Default Vehicle By User Id
    // ----------------------------------------------------

    public async Task<VehicleResponse?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetDefaultByUserIdAsync(
            userId,
            cancellationToken);

        return vehicle is null
            ? null
            : MapToResponse(vehicle);
    }

    // ----------------------------------------------------
    // Update Vehicle
    // ----------------------------------------------------

    public async Task<VehicleResponse?> UpdateAsync(
        Guid id,
        Guid userId,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (vehicle is null ||
            vehicle.UserId != userId ||
            !vehicle.IsActive)
        {
            return null;
        }

        ValidateVehicleData(
            request.PlateNumber,
            request.PlateCode,
            request.Make,
            request.Model,
            request.VehicleType,
            request.ManufacturingYear);

        var plateNumber = request.PlateNumber.Trim();
        var plateCode = NormalizeOptional(request.PlateCode);

        var plateExists = await _vehicleRepository.PlateNumberExistsAsync(
            plateNumber,
            plateCode,
            vehicle.Id,
            cancellationToken);

        if (plateExists)
        {
            throw new InvalidOperationException(
                "A vehicle with this plate number and code already exists.");
        }

        vehicle.PlateNumber = plateNumber;
        vehicle.PlateCode = plateCode;
        vehicle.Make = request.Make.Trim();
        vehicle.Model = request.Model.Trim();
        vehicle.Color = NormalizeOptional(request.Color);
        vehicle.ManufacturingYear = request.ManufacturingYear;
        vehicle.VehicleType = request.VehicleType.Trim();
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        await _vehicleRepository.UpdateAsync(
            vehicle,
            cancellationToken);

        return MapToResponse(vehicle);
    }

    // ----------------------------------------------------
    // Deactivate Vehicle
    // ----------------------------------------------------

    public async Task<bool> DeactivateAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (vehicle is null ||
            vehicle.UserId != userId ||
            !vehicle.IsActive)
        {
            return false;
        }

        vehicle.IsActive = false;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        if (vehicle.IsDefault)
        {
            vehicle.IsDefault = false;

            var otherVehicles =
                await _vehicleRepository.GetActiveByUserIdAsync(
                    userId,
                    cancellationToken);

            var replacement = otherVehicles
                .Where(x => x.Id != vehicle.Id)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.IsDefault = true;
                replacement.UpdatedAt = DateTimeOffset.UtcNow;

                await _vehicleRepository.UpdateAsync(
                    replacement,
                    cancellationToken);
            }
        }

        await _vehicleRepository.UpdateAsync(
            vehicle,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Set Default Vehicle
    // ----------------------------------------------------

    public async Task<VehicleResponse?> SetDefaultAsync(
        Guid vehicleId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(
            vehicleId,
            cancellationToken);

        if (vehicle is null ||
            vehicle.UserId != userId ||
            !vehicle.IsActive)
        {
            return null;
        }

        var userVehicles = await _vehicleRepository.GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        foreach (var existingVehicle in userVehicles.Where(
                     x => x.IsDefault && x.Id != vehicle.Id))
        {
            existingVehicle.IsDefault = false;
            existingVehicle.UpdatedAt = DateTimeOffset.UtcNow;

            await _vehicleRepository.UpdateAsync(
                existingVehicle,
                cancellationToken);
        }

        vehicle.IsDefault = true;
        vehicle.UpdatedAt = DateTimeOffset.UtcNow;

        await _vehicleRepository.UpdateAsync(
            vehicle,
            cancellationToken);

        return MapToResponse(vehicle);
    }

    // ----------------------------------------------------
    // Vehicle Data Validation
    // ----------------------------------------------------

    private static void ValidateVehicleData(
        string plateNumber,
        string? plateCode,
        string make,
        string model,
        string vehicleType,
        int? manufacturingYear)
    {
        if (string.IsNullOrWhiteSpace(plateNumber))
        {
            throw new ArgumentException("PlateNumber is required.");
        }

        if (string.IsNullOrWhiteSpace(make))
        {
            throw new ArgumentException("Make is required.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Model is required.");
        }

        if (string.IsNullOrWhiteSpace(vehicleType))
        {
            throw new ArgumentException("VehicleType is required.");
        }

        if (manufacturingYear.HasValue &&
            (manufacturingYear.Value < 1900 ||
             manufacturingYear.Value > 2100))
        {
            throw new ArgumentException(
                "ManufacturingYear must be between 1900 and 2100.");
        }

        if (plateCode?.Length > 10)
        {
            throw new ArgumentException(
                "PlateCode cannot exceed 10 characters.");
        }
    }

    // ----------------------------------------------------
    // Normalize Optional Values
    // ----------------------------------------------------

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Ensure Valid User Id
    // ----------------------------------------------------

    private static void EnsureValidUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID is required.");
        }
    }

    // ----------------------------------------------------
    // Query Parameters Validation
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        VehicleQueryParameters parameters)
    {
        if (parameters.UserId.HasValue &&
            parameters.UserId.Value == Guid.Empty)
        {
            throw new ArgumentException("User ID is invalid.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.VehicleType?.Length > 100)
        {
            throw new ArgumentException(
                "VehicleType cannot exceed 100 characters.");
        }

        if (parameters.Make?.Length > 100)
        {
            throw new ArgumentException(
                "Make cannot exceed 100 characters.");
        }

        if (parameters.Model?.Length > 100)
        {
            throw new ArgumentException(
                "Model cannot exceed 100 characters.");
        }

        if (parameters.Color?.Length > 50)
        {
            throw new ArgumentException(
                "Color cannot exceed 50 characters.");
        }

        if (parameters.MinManufacturingYear.HasValue &&
            parameters.MinManufacturingYear.Value < 1886)
        {
            throw new ArgumentException(
                "MinManufacturingYear is invalid.");
        }

        if (parameters.MaxManufacturingYear.HasValue &&
            parameters.MaxManufacturingYear.Value < 1886)
        {
            throw new ArgumentException(
                "MaxManufacturingYear is invalid.");
        }

        if (parameters.MinManufacturingYear.HasValue &&
            parameters.MaxManufacturingYear.HasValue &&
            parameters.MinManufacturingYear.Value >
            parameters.MaxManufacturingYear.Value)
        {
            throw new ArgumentException(
                "MinManufacturingYear cannot be greater than MaxManufacturingYear.");
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

    private VehicleResponse MapToResponse(Vehicle vehicle)
    {
        return _mapper.Map<VehicleResponse>(vehicle);
    }

    private PagedResult<VehicleResponse> MapToPagedResponse(
        PagedResult<Vehicle> result)
    {
        return new PagedResult<VehicleResponse>
        {
            Items = _mapper.Map<List<VehicleResponse>>(result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.UserVehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class UserVehicleService : IUserVehicleService
{
    private readonly IUserVehicleRepository _userVehicleRepository;
    private readonly IMapper _mapper;

    public UserVehicleService(
        IUserVehicleRepository userVehicleRepository,
        IMapper mapper)
    {
        _userVehicleRepository = userVehicleRepository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create User Vehicle
    // ----------------------------------------------------

    public async Task<UserVehicleResponse> CreateAsync(
        CreateUserVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        EnsureValidUserId(request.UserId);

        if (request.VehicleId == Guid.Empty)
        {
            throw new ArgumentException("VehicleId is required.");
        }

        var exists = await _userVehicleRepository.ExistsAsync(
            request.UserId,
            request.VehicleId,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException(
                "This vehicle is already linked to this user.");
        }

        var userVehicles = await _userVehicleRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        var shouldBeDefault =
            request.IsDefault || userVehicles.Count == 0;

        if (shouldBeDefault)
        {
            foreach (var existingVehicle in userVehicles.Where(
                         vehicle => vehicle.IsDefault))
            {
                existingVehicle.IsDefault = false;

                await _userVehicleRepository.UpdateAsync(
                    existingVehicle,
                    cancellationToken);
            }
        }

        var userVehicle = new UserVehicle
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            VehicleId = request.VehicleId,
            IsDefault = shouldBeDefault,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _userVehicleRepository.AddAsync(
            userVehicle,
            cancellationToken);

        return MapToResponse(userVehicle);
    }

    // ----------------------------------------------------
    // Get User Vehicle By Id
    // ----------------------------------------------------

    public async Task<UserVehicleResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userVehicle = await _userVehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        return userVehicle is null
            ? null
            : MapToResponse(userVehicle);
    }

    // ----------------------------------------------------
    // Get All User Vehicles
    // ----------------------------------------------------

    public async Task<PagedResult<UserVehicleResponse>> GetAllAsync(
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _userVehicleRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get User Vehicles By User Id
    // ----------------------------------------------------

    public async Task<PagedResult<UserVehicleResponse>> GetByUserIdAsync(
        Guid userId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _userVehicleRepository.GetPagedByUserIdAsync(
            userId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get User Vehicles By Vehicle Id
    // ----------------------------------------------------

    public async Task<PagedResult<UserVehicleResponse>> GetByVehicleIdAsync(
        Guid vehicleId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("VehicleId is required.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _userVehicleRepository.GetPagedByVehicleIdAsync(
            vehicleId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Default User Vehicle
    // ----------------------------------------------------

    public async Task<UserVehicleResponse?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        var userVehicle =
            await _userVehicleRepository.GetDefaultByUserIdAsync(
                userId,
                cancellationToken);

        return userVehicle is null
            ? null
            : MapToResponse(userVehicle);
    }

    // ----------------------------------------------------
    // Update User Vehicle
    // ----------------------------------------------------

    public async Task<UserVehicleResponse?> UpdateAsync(
        Guid id,
        UpdateUserVehicleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userVehicle = await _userVehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (userVehicle is null)
        {
            return null;
        }

        if (request.IsDefault)
        {
            var userVehicles =
                await _userVehicleRepository.GetByUserIdAsync(
                    userVehicle.UserId,
                    cancellationToken);

            foreach (var existingVehicle in userVehicles.Where(
                         vehicle =>
                             vehicle.IsDefault &&
                             vehicle.Id != userVehicle.Id))
            {
                existingVehicle.IsDefault = false;

                await _userVehicleRepository.UpdateAsync(
                    existingVehicle,
                    cancellationToken);
            }
        }
        else
        {
            var currentDefault =
                await _userVehicleRepository.GetDefaultByUserIdAsync(
                    userVehicle.UserId,
                    cancellationToken);

            if (currentDefault?.Id == userVehicle.Id)
            {
                throw new InvalidOperationException(
                    "Choose another default vehicle before removing the current default.");
            }
        }

        userVehicle.IsDefault = request.IsDefault;

        await _userVehicleRepository.UpdateAsync(
            userVehicle,
            cancellationToken);

        return MapToResponse(userVehicle);
    }

    // ----------------------------------------------------
    // Delete User Vehicle
    // ----------------------------------------------------

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userVehicle = await _userVehicleRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (userVehicle is null)
        {
            return false;
        }

        if (userVehicle.IsDefault)
        {
            var userVehicles =
                await _userVehicleRepository.GetByUserIdAsync(
                    userVehicle.UserId,
                    cancellationToken);

            var replacement = userVehicles
                .Where(vehicle => vehicle.Id != userVehicle.Id)
                .OrderByDescending(vehicle => vehicle.CreatedAt)
                .ThenByDescending(vehicle => vehicle.Id)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.IsDefault = true;

                await _userVehicleRepository.UpdateAsync(
                    replacement,
                    cancellationToken);
            }
        }

        await _userVehicleRepository.DeleteAsync(
            userVehicle,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Set Default User Vehicle
    // ----------------------------------------------------

    public async Task<UserVehicleResponse?> SetDefaultAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidUserId(userId);

        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("VehicleId is required.");
        }

        var target = await _userVehicleRepository.GetByUserAndVehicleAsync(
            userId,
            vehicleId,
            cancellationToken);

        if (target is null)
        {
            return null;
        }

        var userVehicles = await _userVehicleRepository.GetByUserIdAsync(
            userId,
            cancellationToken);

        foreach (var existingVehicle in userVehicles.Where(
                     vehicle =>
                         vehicle.IsDefault &&
                         vehicle.Id != target.Id))
        {
            existingVehicle.IsDefault = false;

            await _userVehicleRepository.UpdateAsync(
                existingVehicle,
                cancellationToken);
        }

        target.IsDefault = true;

        await _userVehicleRepository.UpdateAsync(
            target,
            cancellationToken);

        return MapToResponse(target);
    }

    // ----------------------------------------------------
    // Validation Helpers
    // ----------------------------------------------------

    private static void EnsureValidUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.");
        }
    }

    private static void ValidateQueryParameters(
        UserVehicleQueryParameters parameters)
    {
        if (parameters.UserId.HasValue &&
            parameters.UserId.Value == Guid.Empty)
        {
            throw new ArgumentException("UserId is invalid.");
        }

        if (parameters.VehicleId.HasValue &&
            parameters.VehicleId.Value == Guid.Empty)
        {
            throw new ArgumentException("VehicleId is invalid.");
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
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private PagedResult<UserVehicleResponse> MapToPagedResponse(
        PagedResult<UserVehicle> result)
    {
        return new PagedResult<UserVehicleResponse>
        {
            Items = _mapper.Map<List<UserVehicleResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    private UserVehicleResponse MapToResponse(
        UserVehicle userVehicle)
    {
        return _mapper.Map<UserVehicleResponse>(userVehicle);
    }
}
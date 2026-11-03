using AutoMapper;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingDevice;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public sealed class ParkingDeviceService : IParkingDeviceService
{
    private readonly IParkingDeviceRepository _deviceRepository;
    private readonly IMapper _mapper;

    public ParkingDeviceService(
        IParkingDeviceRepository deviceRepository,
        IMapper mapper)
    {
        _deviceRepository = deviceRepository;
        _mapper = mapper;
    }

    // ----------------------------------------------------
    // Create Parking Device
    // ----------------------------------------------------

    public async Task<ParkingDeviceResponse> CreateAsync(
        CreateParkingDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceCode = request.DeviceCode.Trim();

        if (!await _deviceRepository.ParkingGateExistsAsync(
                request.ParkingGateId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The specified parking gate does not exist.");
        }

        if (await _deviceRepository.DeviceCodeExistsAsync(
                deviceCode,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A device with this device code already exists.");
        }

        var device = new ParkingDevice
        {
            ParkingGateId = request.ParkingGateId,
            Name = request.Name.Trim(),
            DeviceCode = deviceCode,
            DeviceType = request.DeviceType.Trim(),
            SerialNumber = NormalizeOptionalValue(request.SerialNumber),
            IpAddress = NormalizeOptionalValue(request.IpAddress),
            IsOnline = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _deviceRepository.AddAsync(
            device,
            cancellationToken);

        return MapToResponse(device);
    }

    // ----------------------------------------------------
    // Get Parking Device By Id
    // ----------------------------------------------------

    public async Task<ParkingDeviceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (device is null || !device.IsActive)
        {
            return null;
        }

        return MapToResponse(device);
    }

    // ----------------------------------------------------
    // Get All Parking Devices
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingDeviceResponse>> GetAllAsync(
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        var result = await _deviceRepository.GetPagedAsync(
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Get Parking Devices By Parking Gate Id
    // ----------------------------------------------------

    public async Task<PagedResult<ParkingDeviceResponse>>
        GetByParkingGateIdAsync(
            Guid parkingGateId,
            ParkingDeviceQueryParameters parameters,
            CancellationToken cancellationToken = default)
    {
        if (parkingGateId == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking gate ID is invalid.");
        }

        ArgumentNullException.ThrowIfNull(parameters);

        ValidateQueryParameters(parameters);

        if (!await _deviceRepository.ParkingGateExistsAsync(
                parkingGateId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The specified parking gate does not exist.");
        }

        var result = await _deviceRepository.GetPagedByParkingGateIdAsync(
            parkingGateId,
            parameters,
            cancellationToken);

        return MapToPagedResponse(result);
    }

    // ----------------------------------------------------
    // Update Parking Device
    // ----------------------------------------------------

    public async Task<ParkingDeviceResponse?> UpdateAsync(
        Guid id,
        UpdateParkingDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (device is null || !device.IsActive)
        {
            return null;
        }

        if (!await _deviceRepository.ParkingGateExistsAsync(
                request.ParkingGateId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "The specified parking gate does not exist.");
        }

        var deviceCode = request.DeviceCode.Trim();

        if (await _deviceRepository.DeviceCodeExistsAsync(
                deviceCode,
                id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A device with this device code already exists.");
        }

        device.ParkingGateId = request.ParkingGateId;
        device.Name = request.Name.Trim();
        device.DeviceCode = deviceCode;
        device.DeviceType = request.DeviceType.Trim();
        device.SerialNumber = NormalizeOptionalValue(
            request.SerialNumber);
        device.IpAddress = NormalizeOptionalValue(
            request.IpAddress);
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _deviceRepository.UpdateAsync(
            device,
            cancellationToken);

        return MapToResponse(device);
    }

    // ----------------------------------------------------
    // Deactivate Parking Device
    // ----------------------------------------------------

    public async Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (device is null || !device.IsActive)
        {
            return false;
        }

        device.IsActive = false;
        device.IsOnline = false;
        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _deviceRepository.UpdateAsync(
            device,
            cancellationToken);

        return true;
    }

    // ----------------------------------------------------
    // Update Parking Device Status
    // ----------------------------------------------------

    public async Task<ParkingDeviceResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingDeviceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var device = await _deviceRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (device is null || !device.IsActive)
        {
            return null;
        }

        device.IsOnline = request.IsOnline;

        if (request.IsOnline)
        {
            device.LastSeenAt = DateTimeOffset.UtcNow;
        }

        device.UpdatedAt = DateTimeOffset.UtcNow;

        await _deviceRepository.UpdateAsync(
            device,
            cancellationToken);

        return MapToResponse(device);
    }

    // ----------------------------------------------------
    // Validate Query Parameters
    // ----------------------------------------------------

    private static void ValidateQueryParameters(
        ParkingDeviceQueryParameters parameters)
    {
        if (parameters.ParkingGateId.HasValue &&
            parameters.ParkingGateId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "Parking gate ID is invalid.");
        }

        if (parameters.SearchTerm?.Length > 200)
        {
            throw new ArgumentException(
                "SearchTerm cannot exceed 200 characters.");
        }

        if (parameters.DeviceType?.Length > 100)
        {
            throw new ArgumentException(
                "DeviceType cannot exceed 100 characters.");
        }

        if (parameters.CreatedFrom.HasValue &&
            parameters.CreatedTo.HasValue &&
            parameters.CreatedFrom.Value > parameters.CreatedTo.Value)
        {
            throw new ArgumentException(
                "CreatedFrom cannot be later than CreatedTo.");
        }

        if (parameters.LastSeenFrom.HasValue &&
            parameters.LastSeenTo.HasValue &&
            parameters.LastSeenFrom.Value > parameters.LastSeenTo.Value)
        {
            throw new ArgumentException(
                "LastSeenFrom cannot be later than LastSeenTo.");
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
    // Normalize Optional Values
    // ----------------------------------------------------

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private ParkingDeviceResponse MapToResponse(
        ParkingDevice device)
    {
        return _mapper.Map<ParkingDeviceResponse>(device);
    }

    private PagedResult<ParkingDeviceResponse> MapToPagedResponse(
        PagedResult<ParkingDevice> result)
    {
        return new PagedResult<ParkingDeviceResponse>
        {
            Items = _mapper.Map<List<ParkingDeviceResponse>>(
                result.Items),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
using AutoMapper;
using Parky.Application.DTOs.DeviceEvent;
using Parky.Application.Interfaces.Repositories;
using Parky.Application.Interfaces.Services;
using Parky.Domain.Entities;

namespace Parky.Infrastructure.Services;

public class DeviceEventService : IDeviceEventService
{
    private readonly IDeviceEventRepository _deviceEventRepository;
    private readonly IParkingDeviceRepository _parkingDeviceRepository;
    private readonly IMapper _mapper;
    private readonly INotificationDispatcher _notificationDispatcher;
    public DeviceEventService(
    IDeviceEventRepository deviceEventRepository,
    IParkingDeviceRepository parkingDeviceRepository,
    IMapper mapper,
    INotificationDispatcher notificationDispatcher)
    {
        _deviceEventRepository = deviceEventRepository;
        _parkingDeviceRepository = parkingDeviceRepository;
        _mapper = mapper;
        _notificationDispatcher = notificationDispatcher;
    }
    // ----------------------------------------------------
    // Get Device Event By Id
    // ----------------------------------------------------

    public async Task<DeviceEventResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var deviceEvent = await _deviceEventRepository.GetByIdAsync(
            id,
            cancellationToken);

        return deviceEvent is null
            ? null
            : MapToResponse(deviceEvent);
    }

    // ----------------------------------------------------
    // Get All Device Events
    // ----------------------------------------------------

    public async Task<IReadOnlyList<DeviceEventResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var events = await _deviceEventRepository.GetAllAsync(
            cancellationToken);

        return _mapper.Map<List<DeviceEventResponse>>(events);
    }

    // ----------------------------------------------------
    // Get Device Events By Parking Device Id
    // ----------------------------------------------------

    public async Task<IReadOnlyList<DeviceEventResponse>>
        GetByParkingDeviceIdAsync(
            Guid parkingDeviceId,
            CancellationToken cancellationToken = default)
    {
        if (!await _parkingDeviceRepository.ExistsAsync(parkingDeviceId))
        {
            throw new KeyNotFoundException(
                "The specified parking device was not found.");
        }

        var events = await _deviceEventRepository.GetByParkingDeviceIdAsync(
            parkingDeviceId,
            cancellationToken);

        return _mapper.Map<List<DeviceEventResponse>>(events);
    }

    // ----------------------------------------------------
    // Get Device Events By Event Type
    // ----------------------------------------------------

    public async Task<IReadOnlyList<DeviceEventResponse>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException(
                "Event type is required.",
                nameof(eventType));
        }

        var events = await _deviceEventRepository.GetByEventTypeAsync(
            eventType.Trim(),
            cancellationToken);

        return _mapper.Map<List<DeviceEventResponse>>(events);
    }

    // ----------------------------------------------------
    // Get Device Events By Date Range
    // ----------------------------------------------------

    public async Task<IReadOnlyList<DeviceEventResponse>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        if (from > to)
        {
            throw new ArgumentException(
                "The start date must be earlier than or equal to the end date.");
        }

        var events = await _deviceEventRepository.GetByDateRangeAsync(
            from,
            to,
            cancellationToken);

        return _mapper.Map<List<DeviceEventResponse>>(events);
    }

    // ----------------------------------------------------
    // Create Device Event
    // ----------------------------------------------------

    public async Task<DeviceEventResponse> CreateAsync(
        CreateDeviceEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ParkingDeviceId == Guid.Empty)
        {
            throw new ArgumentException("Parking device ID is required.");
        }

        if (!await _parkingDeviceRepository.ExistsAsync(
                request.ParkingDeviceId))
        {
            throw new KeyNotFoundException(
                "The specified parking device was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            throw new ArgumentException("Event type is required.");
        }

        if (string.IsNullOrWhiteSpace(request.EventStatus))
        {
            throw new ArgumentException("Event status is required.");
        }

        if (request.EventType.Trim().Length > 100)
        {
            throw new ArgumentException(
                "Event type cannot exceed 100 characters.");
        }

        if (request.EventStatus.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Event status cannot exceed 50 characters.");
        }

        if (request.Description?.Length > 1000)
        {
            throw new ArgumentException(
                "Description cannot exceed 1000 characters.");
        }

        var receivedAt = DateTimeOffset.UtcNow;

        var deviceEvent = new DeviceEvent
        {
            Id = Guid.NewGuid(),
            ParkingDeviceId = request.ParkingDeviceId,
            EventType = request.EventType.Trim(),
            EventStatus = request.EventStatus.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Payload = request.Payload,
            OccurredAt = request.OccurredAt == default
                ? receivedAt
                : request.OccurredAt,
            ReceivedAt = receivedAt
        };

        await _deviceEventRepository.AddAsync(
            deviceEvent,
            cancellationToken);

        var ownerId = await _parkingDeviceRepository
        .GetParkingOwnerIdByDeviceIdAsync(
        deviceEvent.ParkingDeviceId,
        cancellationToken);

        if (ownerId.HasValue && ownerId.Value != Guid.Empty)
        {
            await _notificationDispatcher.DispatchAsync(
                ownerId.Value,
                "Parking Device Event",
                $"Device event: {deviceEvent.EventType}. " +
                $"Status: {deviceEvent.EventStatus}. " +
                $"Details: {deviceEvent.Description}",
                "DeviceEvent",
                deviceEvent.Id.ToString(),
                cancellationToken: cancellationToken);
        }
        return MapToResponse(deviceEvent);
    }

    // ----------------------------------------------------
    // Mapping
    // ----------------------------------------------------

    private DeviceEventResponse MapToResponse(
        DeviceEvent deviceEvent)
    {
        return _mapper.Map<DeviceEventResponse>(deviceEvent);
    }
}
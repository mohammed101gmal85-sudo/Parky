using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.DTOs.DeviceEvent;

namespace Parky.Application.Interfaces.Services;

public interface IDeviceEventService
{
    Task<DeviceEventResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEventResponse>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEventResponse>> GetByParkingDeviceIdAsync(
        Guid parkingDeviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEventResponse>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEventResponse>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<DeviceEventResponse> CreateAsync(
        CreateDeviceEventRequest request,
        CancellationToken cancellationToken = default);
}
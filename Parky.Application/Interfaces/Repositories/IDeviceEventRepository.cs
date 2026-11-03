using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IDeviceEventRepository
{
    Task<DeviceEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEvent>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEvent>> GetByParkingDeviceIdAsync(
        Guid parkingDeviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEvent>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceEvent>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DeviceEvent deviceEvent,
        CancellationToken cancellationToken = default);
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class DeviceEventRepository : IDeviceEventRepository
{
    private readonly ParkyDbContext _dbContext;

    public DeviceEventRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DeviceEvent?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DeviceEvent>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceEvent>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DeviceEvent>()
            .AsNoTracking()
            .OrderByDescending(x => x.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceEvent>> GetByParkingDeviceIdAsync(
        Guid parkingDeviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DeviceEvent>()
            .AsNoTracking()
            .Where(x => x.ParkingDeviceId == parkingDeviceId)
            .OrderByDescending(x => x.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceEvent>> GetByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DeviceEvent>()
            .AsNoTracking()
            .Where(x => x.EventType == eventType)
            .OrderByDescending(x => x.ReceivedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceEvent>> GetByDateRangeAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DeviceEvent>()
            .AsNoTracking()
            .Where(x => x.OccurredAt >= from && x.OccurredAt <= to)
            .OrderByDescending(x => x.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        DeviceEvent deviceEvent,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<DeviceEvent>()
            .AddAsync(deviceEvent, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
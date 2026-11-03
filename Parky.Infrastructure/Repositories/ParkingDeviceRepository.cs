using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingDevice;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingDeviceRepository : IParkingDeviceRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingDeviceRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingDevice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                device => device.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingDevice>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .OrderBy(device => device.Name)
            .ThenBy(device => device.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingDevice>> GetPagedAsync(
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .Where(device => device.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingDevice>> GetByParkingGateIdAsync(
        Guid parkingGateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .Where(device => device.ParkingGateId == parkingGateId)
            .OrderBy(device => device.Name)
            .ThenBy(device => device.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingDevice>> GetPagedByParkingGateIdAsync(
        Guid parkingGateId,
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .Where(device =>
                device.ParkingGateId == parkingGateId &&
                device.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<ParkingDevice?> GetByDeviceCodeAsync(
        string deviceCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = deviceCode.Trim();

        return await _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                device => device.DeviceCode == normalizedCode,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingDevice>()
            .AnyAsync(
                device => device.Id == id,
                cancellationToken);
    }

    public async Task<Guid?> GetParkingOwnerIdByDeviceIdAsync(
        Guid parkingDeviceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .Where(device => device.Id == parkingDeviceId)
            .Select(device => (Guid?)device.ParkingGate.Parking.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> DeviceCodeExistsAsync(
        string deviceCode,
        Guid? excludedDeviceId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = deviceCode.Trim();

        var query = _dbContext.Set<ParkingDevice>()
            .AsNoTracking()
            .Where(device => device.DeviceCode == normalizedCode);

        if (excludedDeviceId.HasValue)
        {
            query = query.Where(
                device => device.Id != excludedDeviceId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ParkingGateExistsAsync(
        Guid parkingGateId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingGate>()
            .AnyAsync(
                gate => gate.Id == parkingGateId,
                cancellationToken);
    }

    public async Task AddAsync(
        ParkingDevice device,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingDevice>()
            .AddAsync(device, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingDevice device,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingDevice>().Update(device);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingDevice> ApplyFilters(
        IQueryable<ParkingDevice> query,
        ParkingDeviceQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(device =>
                device.Name.ToLower().Contains(searchTerm) ||
                device.DeviceCode.ToLower().Contains(searchTerm) ||
                device.DeviceType.ToLower().Contains(searchTerm) ||
                (device.SerialNumber != null &&
                 device.SerialNumber.ToLower().Contains(searchTerm)) ||
                (device.IpAddress != null &&
                 device.IpAddress.ToLower().Contains(searchTerm)));
        }

        if (parameters.ParkingGateId.HasValue)
        {
            query = query.Where(device =>
                device.ParkingGateId == parameters.ParkingGateId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.DeviceType))
        {
            var deviceType = parameters.DeviceType.Trim().ToLower();

            query = query.Where(device =>
                device.DeviceType.ToLower() == deviceType);
        }

        if (parameters.IsOnline.HasValue)
        {
            query = query.Where(device =>
                device.IsOnline == parameters.IsOnline.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(device =>
                device.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(device =>
                device.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.LastSeenFrom.HasValue)
        {
            query = query.Where(device =>
                device.LastSeenAt.HasValue &&
                device.LastSeenAt.Value >= parameters.LastSeenFrom.Value);
        }

        if (parameters.LastSeenTo.HasValue)
        {
            query = query.Where(device =>
                device.LastSeenAt.HasValue &&
                device.LastSeenAt.Value <= parameters.LastSeenTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(device =>
                device.UpdatedAt.HasValue &&
                device.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(device =>
                device.UpdatedAt.HasValue &&
                device.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingDevice>> CreatePagedResultAsync(
        IQueryable<ParkingDevice> query,
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(device => device.Name)
            .ThenBy(device => device.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingDevice>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
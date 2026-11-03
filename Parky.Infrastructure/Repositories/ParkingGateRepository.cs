
using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.ParkingGate;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public sealed class ParkingGateRepository : IParkingGateRepository
{
    private readonly ParkyDbContext _dbContext;

    public ParkingGateRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ParkingGate?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                gate => gate.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingGate>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .OrderBy(gate => gate.Name)
            .ThenBy(gate => gate.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingGate>> GetPagedAsync(
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .Where(gate => gate.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ParkingGate>> GetByParkingIdAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .Where(gate =>
                gate.ParkingId == parkingId &&
                gate.IsActive)
            .OrderBy(gate => gate.Name)
            .ThenBy(gate => gate.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ParkingGate>> GetPagedByParkingIdAsync(
        Guid parkingId,
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .Where(gate =>
                gate.ParkingId == parkingId &&
                gate.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken);
    }

    public async Task<ParkingGate?> GetByGateCodeAsync(
        string gateCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = gateCode.Trim();

        return await _dbContext.Set<ParkingGate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                gate => gate.GateCode == normalizedCode,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<ParkingGate>()
            .AnyAsync(
                gate => gate.Id == id,
                cancellationToken);
    }

    public async Task<bool> ParkingExistsAsync(
        Guid parkingId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Parking>()
            .AnyAsync(
                parking =>
                    parking.Id == parkingId &&
                    parking.IsActive,
                cancellationToken);
    }

    public async Task<bool> ParkingBelongsToOwnerAsync(
        Guid parkingId,
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Parking>()
            .AnyAsync(
                parking =>
                    parking.Id == parkingId &&
                    parking.OwnerId == ownerId &&
                    parking.IsActive,
                cancellationToken);
    }

    public async Task<bool> GateCodeExistsAsync(
        string gateCode,
        Guid? excludedGateId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = gateCode.Trim();

        var query = _dbContext.Set<ParkingGate>()
            .Where(gate => gate.GateCode == normalizedCode);

        if (excludedGateId.HasValue)
        {
            query = query.Where(
                gate => gate.Id != excludedGateId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(
        ParkingGate gate,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ParkingGate>()
            .AddAsync(gate, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        ParkingGate gate,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<ParkingGate>().Update(gate);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ParkingGate> ApplyFilters(
        IQueryable<ParkingGate> query,
        ParkingGateQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm
                .Trim()
                .ToLower();

            query = query.Where(gate =>
                gate.Name.ToLower().Contains(searchTerm) ||
                gate.GateCode.ToLower().Contains(searchTerm) ||
                gate.GateType.ToLower().Contains(searchTerm) ||
                (gate.LocationDescription != null &&
                 gate.LocationDescription.ToLower().Contains(searchTerm)));
        }

        if (parameters.ParkingId.HasValue)
        {
            query = query.Where(gate =>
                gate.ParkingId == parameters.ParkingId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.GateType))
        {
            var gateType = parameters.GateType
                .Trim()
                .ToLower();

            query = query.Where(gate =>
                gate.GateType.ToLower() == gateType);
        }

        if (parameters.IsEntryGate.HasValue)
        {
            query = query.Where(gate =>
                gate.IsEntryGate == parameters.IsEntryGate.Value);
        }

        if (parameters.IsExitGate.HasValue)
        {
            query = query.Where(gate =>
                gate.IsExitGate == parameters.IsExitGate.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(gate =>
                gate.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(gate =>
                gate.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(gate =>
                gate.UpdatedAt.HasValue &&
                gate.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(gate =>
                gate.UpdatedAt.HasValue &&
                gate.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<ParkingGate>> CreatePagedResultAsync(
        IQueryable<ParkingGate> query,
        ParkingGateQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(gate => gate.Name)
            .ThenBy(gate => gate.Id)
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkingGate>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}

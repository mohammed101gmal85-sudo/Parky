using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.Vehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly ParkyDbContext _dbContext;

    public VehicleRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Vehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vehicle => vehicle.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .OrderBy(vehicle => vehicle.PlateNumber)
            .ThenBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Vehicle>> GetPagedAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: false);
    }

    public async Task<IReadOnlyList<Vehicle>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .Where(vehicle => vehicle.UserId == userId)
            .OrderByDescending(vehicle => vehicle.IsDefault)
            .ThenByDescending(vehicle => vehicle.CreatedAt)
            .ThenBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Vehicle>> GetPagedByUserIdAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .Where(vehicle => vehicle.UserId == userId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: true);
    }

    public async Task<IReadOnlyList<Vehicle>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.UserId == userId &&
                vehicle.IsActive)
            .OrderByDescending(vehicle => vehicle.IsDefault)
            .ThenByDescending(vehicle => vehicle.CreatedAt)
            .ThenBy(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Vehicle>> GetPagedActiveByUserIdAsync(
        Guid userId,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .Where(vehicle =>
                vehicle.UserId == userId &&
                vehicle.IsActive);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: true);
    }

    public async Task<Vehicle?> GetByPlateNumberAsync(
        string plateNumber,
        string? plateCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlateNumber = plateNumber.Trim();
        var normalizedPlateCode = plateCode?.Trim();

        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vehicle =>
                    vehicle.PlateNumber == normalizedPlateNumber &&
                    vehicle.PlateCode == normalizedPlateCode,
                cancellationToken);
    }

    public async Task<bool> PlateNumberExistsAsync(
        string plateNumber,
        string? plateCode,
        Guid? excludedVehicleId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPlateNumber = plateNumber.Trim();
        var normalizedPlateCode = plateCode?.Trim();

        return await _dbContext.Set<Vehicle>()
            .AnyAsync(
                vehicle =>
                    vehicle.PlateNumber == normalizedPlateNumber &&
                    vehicle.PlateCode == normalizedPlateCode &&
                    (!excludedVehicleId.HasValue ||
                     vehicle.Id != excludedVehicleId.Value),
                cancellationToken);
    }

    public async Task<Vehicle?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Vehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                vehicle =>
                    vehicle.UserId == userId &&
                    vehicle.IsDefault &&
                    vehicle.IsActive,
                cancellationToken);
    }

    public async Task AddAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Vehicle>()
            .AddAsync(vehicle, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Vehicle>().Update(vehicle);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Vehicle vehicle,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<Vehicle>().Remove(vehicle);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Vehicle> ApplyFilters(
        IQueryable<Vehicle> query,
        VehicleQueryParameters parameters)
    {
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(vehicle =>
                vehicle.PlateNumber.ToLower().Contains(searchTerm) ||
                (vehicle.PlateCode != null &&
                 vehicle.PlateCode.ToLower().Contains(searchTerm)) ||
                vehicle.Make.ToLower().Contains(searchTerm) ||
                vehicle.Model.ToLower().Contains(searchTerm) ||
                (vehicle.Color != null &&
                 vehicle.Color.ToLower().Contains(searchTerm)) ||
                vehicle.VehicleType.ToLower().Contains(searchTerm));
        }

        if (parameters.UserId.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.UserId == parameters.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.VehicleType))
        {
            var vehicleType = parameters.VehicleType.Trim().ToLower();

            query = query.Where(vehicle =>
                vehicle.VehicleType.ToLower() == vehicleType);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            var make = parameters.Make.Trim().ToLower();

            query = query.Where(vehicle =>
                vehicle.Make.ToLower().Contains(make));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            var model = parameters.Model.Trim().ToLower();

            query = query.Where(vehicle =>
                vehicle.Model.ToLower().Contains(model));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Color))
        {
            var color = parameters.Color.Trim().ToLower();

            query = query.Where(vehicle =>
                vehicle.Color != null &&
                vehicle.Color.ToLower().Contains(color));
        }

        if (parameters.IsDefault.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.IsDefault == parameters.IsDefault.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.IsActive == parameters.IsActive.Value);
        }

        if (parameters.MinManufacturingYear.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.ManufacturingYear.HasValue &&
                vehicle.ManufacturingYear.Value >=
                parameters.MinManufacturingYear.Value);
        }

        if (parameters.MaxManufacturingYear.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.ManufacturingYear.HasValue &&
                vehicle.ManufacturingYear.Value <=
                parameters.MaxManufacturingYear.Value);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.CreatedAt <= parameters.CreatedTo.Value);
        }

        if (parameters.UpdatedFrom.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.UpdatedAt.HasValue &&
                vehicle.UpdatedAt.Value >= parameters.UpdatedFrom.Value);
        }

        if (parameters.UpdatedTo.HasValue)
        {
            query = query.Where(vehicle =>
                vehicle.UpdatedAt.HasValue &&
                vehicle.UpdatedAt.Value <= parameters.UpdatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<Vehicle>> CreatePagedResultAsync(
        IQueryable<Vehicle> query,
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken,
        bool orderByDefault)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = orderByDefault
            ? query
                .OrderByDescending(vehicle => vehicle.IsDefault)
                .ThenByDescending(vehicle => vehicle.CreatedAt)
                .ThenBy(vehicle => vehicle.Id)
            : query
                .OrderBy(vehicle => vehicle.PlateNumber)
                .ThenBy(vehicle => vehicle.Id);

        var items = await orderedQuery
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Vehicle>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
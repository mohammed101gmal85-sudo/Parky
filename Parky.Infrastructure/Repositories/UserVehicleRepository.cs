using Microsoft.EntityFrameworkCore;
using Parky.Application.Common;
using Parky.Application.DTOs.UserVehicle;
using Parky.Application.Interfaces.Repositories;
using Parky.Domain.Entities;
using Parky.Infrastructure.Persistence;

namespace Parky.Infrastructure.Repositories;

public class UserVehicleRepository : IUserVehicleRepository
{
    private readonly ParkyDbContext _dbContext;

    public UserVehicleRepository(ParkyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserVehicle?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userVehicle => userVehicle.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<UserVehicle>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .OrderByDescending(userVehicle => userVehicle.CreatedAt)
            .ThenByDescending(userVehicle => userVehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserVehicle>> GetPagedAsync(
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: false);
    }

    public async Task<IReadOnlyList<UserVehicle>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .Where(userVehicle => userVehicle.UserId == userId)
            .OrderByDescending(userVehicle => userVehicle.IsDefault)
            .ThenByDescending(userVehicle => userVehicle.CreatedAt)
            .ThenByDescending(userVehicle => userVehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserVehicle>> GetPagedByUserIdAsync(
        Guid userId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .Where(userVehicle => userVehicle.UserId == userId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: true);
    }

    public async Task<IReadOnlyList<UserVehicle>> GetByVehicleIdAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .Where(userVehicle => userVehicle.VehicleId == vehicleId)
            .OrderByDescending(userVehicle => userVehicle.CreatedAt)
            .ThenByDescending(userVehicle => userVehicle.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<UserVehicle>> GetPagedByVehicleIdAsync(
        Guid vehicleId,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .Where(userVehicle => userVehicle.VehicleId == vehicleId);

        query = ApplyFilters(query, parameters);

        return await CreatePagedResultAsync(
            query,
            parameters,
            cancellationToken,
            orderByDefault: false);
    }

    public async Task<UserVehicle?> GetByUserAndVehicleAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userVehicle =>
                    userVehicle.UserId == userId &&
                    userVehicle.VehicleId == vehicleId,
                cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid userId,
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AnyAsync(
                userVehicle =>
                    userVehicle.UserId == userId &&
                    userVehicle.VehicleId == vehicleId,
                cancellationToken);
    }

    public async Task<UserVehicle?> GetDefaultByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UserVehicle>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                userVehicle =>
                    userVehicle.UserId == userId &&
                    userVehicle.IsDefault,
                cancellationToken);
    }

    public async Task AddAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<UserVehicle>()
            .AddAsync(userVehicle, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<UserVehicle>().Update(userVehicle);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        UserVehicle userVehicle,
        CancellationToken cancellationToken = default)
    {
        _dbContext.Set<UserVehicle>().Remove(userVehicle);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<UserVehicle> ApplyFilters(
        IQueryable<UserVehicle> query,
        UserVehicleQueryParameters parameters)
    {
        var vehicles = _dbContext.Set<Vehicle>()
            .AsNoTracking();

        if (parameters.UserId.HasValue)
        {
            query = query.Where(userVehicle =>
                userVehicle.UserId == parameters.UserId.Value);
        }

        if (parameters.VehicleId.HasValue)
        {
            query = query.Where(userVehicle =>
                userVehicle.VehicleId == parameters.VehicleId.Value);
        }

        if (parameters.IsDefault.HasValue)
        {
            query = query.Where(userVehicle =>
                userVehicle.IsDefault == parameters.IsDefault.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();

            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    (
                        vehicle.PlateNumber.ToLower().Contains(searchTerm) ||
                        (vehicle.PlateCode != null &&
                         vehicle.PlateCode.ToLower().Contains(searchTerm)) ||
                        vehicle.Make.ToLower().Contains(searchTerm) ||
                        vehicle.Model.ToLower().Contains(searchTerm) ||
                        (vehicle.Color != null &&
                         vehicle.Color.ToLower().Contains(searchTerm)) ||
                        vehicle.VehicleType.ToLower().Contains(searchTerm)
                    )));
        }

        if (parameters.VehicleIsActive.HasValue)
        {
            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.IsActive == parameters.VehicleIsActive.Value));
        }

        if (!string.IsNullOrWhiteSpace(parameters.VehicleType))
        {
            var vehicleType = parameters.VehicleType.Trim().ToLower();

            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.VehicleType.ToLower() == vehicleType));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Make))
        {
            var make = parameters.Make.Trim().ToLower();

            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.Make.ToLower().Contains(make)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Model))
        {
            var model = parameters.Model.Trim().ToLower();

            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.Model.ToLower().Contains(model)));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Color))
        {
            var color = parameters.Color.Trim().ToLower();

            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.Color != null &&
                    vehicle.Color.ToLower().Contains(color)));
        }

        if (parameters.MinManufacturingYear.HasValue)
        {
            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.ManufacturingYear.HasValue &&
                    vehicle.ManufacturingYear.Value >=
                    parameters.MinManufacturingYear.Value));
        }

        if (parameters.MaxManufacturingYear.HasValue)
        {
            query = query.Where(userVehicle =>
                vehicles.Any(vehicle =>
                    vehicle.Id == userVehicle.VehicleId &&
                    vehicle.ManufacturingYear.HasValue &&
                    vehicle.ManufacturingYear.Value <=
                    parameters.MaxManufacturingYear.Value));
        }

        if (parameters.CreatedFrom.HasValue)
        {
            query = query.Where(userVehicle =>
                userVehicle.CreatedAt >= parameters.CreatedFrom.Value);
        }

        if (parameters.CreatedTo.HasValue)
        {
            query = query.Where(userVehicle =>
                userVehicle.CreatedAt <= parameters.CreatedTo.Value);
        }

        return query;
    }

    private static async Task<PagedResult<UserVehicle>> CreatePagedResultAsync(
        IQueryable<UserVehicle> query,
        UserVehicleQueryParameters parameters,
        CancellationToken cancellationToken,
        bool orderByDefault)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var orderedQuery = orderByDefault
            ? query
                .OrderByDescending(userVehicle => userVehicle.IsDefault)
                .ThenByDescending(userVehicle => userVehicle.CreatedAt)
                .ThenByDescending(userVehicle => userVehicle.Id)
            : query
                .OrderByDescending(userVehicle => userVehicle.CreatedAt)
                .ThenByDescending(userVehicle => userVehicle.Id);

        var items = await orderedQuery
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<UserVehicle>
        {
            Items = items,
            PageNumber = parameters.PageNumber,
            PageSize = parameters.PageSize,
            TotalCount = totalCount
        };
    }
}
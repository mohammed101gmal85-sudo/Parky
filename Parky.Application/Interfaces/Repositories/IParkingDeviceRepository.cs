using Parky.Application.Common;
using Parky.Application.DTOs.ParkingDevice;
using Parky.Domain.Entities;

namespace Parky.Application.Interfaces.Repositories;

public interface IParkingDeviceRepository
{
    Task<ParkingDevice?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingDevice>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingDevice>> GetPagedAsync(
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParkingDevice>> GetByParkingGateIdAsync(
        Guid parkingGateId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingDevice>> GetPagedByParkingGateIdAsync(
        Guid parkingGateId,
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingDevice?> GetByDeviceCodeAsync(
        string deviceCode,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetParkingOwnerIdByDeviceIdAsync(
        Guid parkingDeviceId,
        CancellationToken cancellationToken = default);

    Task<bool> DeviceCodeExistsAsync(
        string deviceCode,
        Guid? excludedDeviceId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ParkingDevice device,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ParkingDevice device,
        CancellationToken cancellationToken = default);

    Task<bool> ParkingGateExistsAsync(
        Guid parkingGateId,
        CancellationToken cancellationToken = default);
}
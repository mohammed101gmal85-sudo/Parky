using Parky.Application.Common;
using Parky.Application.DTOs.ParkingDevice;

namespace Parky.Application.Interfaces.Services;

public interface IParkingDeviceService
{
    Task<ParkingDeviceResponse> CreateAsync(
        CreateParkingDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<ParkingDeviceResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingDeviceResponse>> GetAllAsync(
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ParkingDeviceResponse>> GetByParkingGateIdAsync(
        Guid parkingGateId,
        ParkingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<ParkingDeviceResponse?> UpdateAsync(
        Guid id,
        UpdateParkingDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ParkingDeviceResponse?> UpdateStatusAsync(
        Guid id,
        UpdateParkingDeviceStatusRequest request,
        CancellationToken cancellationToken = default);
}
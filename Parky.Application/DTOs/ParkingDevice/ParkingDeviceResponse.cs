using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Application.DTOs.ParkingDevice;

public sealed class ParkingDeviceResponse
{
    public Guid Id { get; init; }

    public Guid ParkingGateId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string DeviceCode { get; init; } = string.Empty;

    public string DeviceType { get; init; } = string.Empty;

    public string? SerialNumber { get; init; }

    public string? IpAddress { get; init; }

    public bool IsOnline { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset? LastSeenAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

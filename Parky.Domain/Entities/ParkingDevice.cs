using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ParkingDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParkingGateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DeviceCode { get; set; } = string.Empty;

    public string DeviceType { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? IpAddress { get; set; }

    public bool IsOnline { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ParkingGate ParkingGate { get; set; } = null!;

    public ICollection<DeviceEvent> DeviceEvents { get; set; } =
        new List<DeviceEvent>();

}

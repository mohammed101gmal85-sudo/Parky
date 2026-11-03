using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ParkingGate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParkingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string GateCode { get; set; } = string.Empty;

    public string GateType { get; set; } = string.Empty;

    public string? LocationDescription { get; set; }

    public bool IsEntryGate { get; set; }

    public bool IsExitGate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Parking Parking { get; set; } = null!;

    public ICollection<ParkingDevice> Devices { get; set; } =
        new List<ParkingDevice>();
}


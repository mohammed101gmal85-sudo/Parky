using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingGate;

public sealed class ParkingGateResponse
{
    public Guid Id { get; init; }

    public Guid ParkingId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string GateCode { get; init; } = string.Empty;

    public string GateType { get; init; } = string.Empty;

    public string? LocationDescription { get; init; }

    public bool IsEntryGate { get; init; }

    public bool IsExitGate { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

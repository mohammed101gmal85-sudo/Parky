using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingFloor;

public sealed class ParkingFloorResponse
{
    public Guid Id { get; init; }

    public Guid ParkingId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int FloorNumber { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

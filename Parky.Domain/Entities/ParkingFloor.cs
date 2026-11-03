using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ParkingFloor
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParkingId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int FloorNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Parking Parking { get; set; } = null!;

    public ICollection<ParkingSpot> Spots { get; set; } =
        new List<ParkingSpot>();
}


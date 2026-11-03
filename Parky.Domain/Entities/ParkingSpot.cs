using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ParkingSpot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParkingFloorId { get; set; }

    public string SpotNumber { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? XCoordinate { get; set; }

    public decimal? YCoordinate { get; set; }

    public decimal? Width { get; set; }

    public decimal? Height { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ParkingFloor ParkingFloor { get; set; } = null!;

    public Guid? ParkingSpotTypeId { get; set; }

    public ParkingSpotType? ParkingSpotType { get; set; }

}


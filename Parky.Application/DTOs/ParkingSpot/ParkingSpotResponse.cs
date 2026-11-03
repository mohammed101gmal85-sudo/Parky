using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingSpot;

public sealed class ParkingSpotResponse
{
    public Guid Id { get; init; }

    public Guid ParkingFloorId { get; init; }

    public string SpotNumber { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal? XCoordinate { get; init; }

    public decimal? YCoordinate { get; init; }

    public decimal? Width { get; init; }

    public decimal? Height { get; init; }

    public bool IsAvailable { get; init; }

    public bool IsActive { get; init; }

    public Guid? ParkingSpotTypeId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}


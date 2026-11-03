using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ParkingFloor;

public sealed class CreateParkingFloorRequest
{
    [Required]
    public Guid ParkingId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(-10, 200)]
    public int FloorNumber { get; init; }
}

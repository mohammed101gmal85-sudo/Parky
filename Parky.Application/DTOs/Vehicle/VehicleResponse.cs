using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.Vehicle;

public class VehicleResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public string? PlateCode { get; set; }

    public string Make { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string? Color { get; set; }

    public int? ManufacturingYear { get; set; }

    public string VehicleType { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}


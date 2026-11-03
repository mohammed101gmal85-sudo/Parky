using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.Vehicle;

public class CreateVehicleRequest
{
    [Required]
    [StringLength(20, MinimumLength = 2)]
    public string PlateNumber { get; set; } = string.Empty;

    [StringLength(10)]
    public string? PlateCode { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string Make { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string Model { get; set; } = string.Empty;

    [StringLength(30)]
    public string? Color { get; set; }

    [Range(1900, 2100)]
    public int? ManufacturingYear { get; set; }

    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string VehicleType { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}

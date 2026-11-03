using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ParkingDevice;

public sealed class CreateParkingDeviceRequest
{
    [Required]
    public Guid ParkingGateId { get; init; }

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string DeviceCode { get; init; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string DeviceType { get; init; } = string.Empty;

    [StringLength(100)]
    public string? SerialNumber { get; init; }

    [StringLength(45)]
    public string? IpAddress { get; init; }
}

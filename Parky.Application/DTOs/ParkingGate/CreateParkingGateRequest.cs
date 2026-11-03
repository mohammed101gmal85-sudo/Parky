using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ParkingGate;

public sealed class CreateParkingGateRequest
{
    [Required]
    public Guid ParkingId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string GateCode { get; init; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 2)]
    public string GateType { get; init; } = string.Empty;

    [StringLength(250)]
    public string? LocationDescription { get; init; }

    public bool IsEntryGate { get; init; }

    public bool IsExitGate { get; init; }
}


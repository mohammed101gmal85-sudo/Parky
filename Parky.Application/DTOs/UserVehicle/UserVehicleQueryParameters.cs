using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.UserVehicle;

public sealed class UserVehicleQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? UserId { get; set; }

    public Guid? VehicleId { get; set; }

    public bool? IsDefault { get; set; }

    public bool? VehicleIsActive { get; set; }

    public string? VehicleType { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public int? MinManufacturingYear { get; set; }

    public int? MaxManufacturingYear { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }
}

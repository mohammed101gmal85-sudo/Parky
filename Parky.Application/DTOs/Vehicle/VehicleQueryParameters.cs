using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.Vehicle;

public sealed class VehicleQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? UserId { get; set; }

    public string? VehicleType { get; set; }

    public string? Make { get; set; }

    public string? Model { get; set; }

    public string? Color { get; set; }

    public bool? IsDefault { get; set; }

    public bool? IsActive { get; set; }

    public int? MinManufacturingYear { get; set; }

    public int? MaxManufacturingYear { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? UpdatedFrom { get; set; }

    public DateTimeOffset? UpdatedTo { get; set; }
}

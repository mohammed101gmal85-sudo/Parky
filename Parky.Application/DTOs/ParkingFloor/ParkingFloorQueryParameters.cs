using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingFloor;

public sealed class ParkingFloorQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ParkingId { get; set; }

    public int? FloorNumber { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? UpdatedFrom { get; set; }

    public DateTimeOffset? UpdatedTo { get; set; }
}

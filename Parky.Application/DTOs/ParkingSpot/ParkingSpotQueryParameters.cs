using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingSpot;

public sealed class ParkingSpotQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ParkingFloorId { get; set; }

    public Guid? ParkingSpotTypeId { get; set; }

    public bool? IsAvailable { get; set; }

    public bool? IsActive { get; set; }
}

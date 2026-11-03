using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.Parking;

public sealed class ParkingQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public string? Governorate { get; set; }

    public string? City { get; set; }
}

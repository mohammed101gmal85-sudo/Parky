using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingSpotType;

public class ParkingSpotTypeQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public bool? IsActive { get; set; }

    public decimal? MinAdditionalHourlyRate { get; set; }

    public decimal? MaxAdditionalHourlyRate { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }
}


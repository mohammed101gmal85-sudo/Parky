using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingImage;

public class ParkingImageQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ParkingId { get; set; }

    public bool? IsPrimary { get; set; }

    public int? MinDisplayOrder { get; set; }

    public int? MaxDisplayOrder { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.ParkingOperatingHour;

public class ParkingOperatingHourQueryParameters : PaginationRequest
{
    public Guid? ParkingId { get; set; }

    public DayOfWeek? DayOfWeek { get; set; }

    public bool? IsClosed { get; set; }

    public bool? IsOpen24Hours { get; set; }

    public TimeOnly? OpenTimeFrom { get; set; }

    public TimeOnly? OpenTimeTo { get; set; }

    public TimeOnly? CloseTimeFrom { get; set; }

    public TimeOnly? CloseTimeTo { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }

    public DateTimeOffset? UpdatedFrom { get; set; }

    public DateTimeOffset? UpdatedTo { get; set; }
}

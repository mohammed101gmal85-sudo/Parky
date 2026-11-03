using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Parky.Application.Common;

namespace Parky.Application.DTOs.Reservation;

public sealed class ReservationQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public string? Status { get; set; }

    public Guid? ParkingId { get; set; }

    public DateTimeOffset? FromDate { get; set; }

    public DateTimeOffset? ToDate { get; set; }
}

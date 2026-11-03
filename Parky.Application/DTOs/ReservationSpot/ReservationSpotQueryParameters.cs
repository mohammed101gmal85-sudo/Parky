using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parky.Application.Common;

namespace Parky.Application.DTOs.ReservationSpot;

public sealed class ReservationSpotQueryParameters : PaginationRequest
{
    public string? SearchTerm { get; set; }

    public Guid? ReservationId { get; set; }

    public Guid? ParkingSpotId { get; set; }

    public string? ReservationStatus { get; set; }

    public decimal? MinPricePerHour { get; set; }

    public decimal? MaxPricePerHour { get; set; }

    public decimal? MinReservedAmount { get; set; }

    public decimal? MaxReservedAmount { get; set; }

    public DateTimeOffset? CreatedFrom { get; set; }

    public DateTimeOffset? CreatedTo { get; set; }
}

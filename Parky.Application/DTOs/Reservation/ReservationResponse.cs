using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.Reservation;

public class ReservationResponse
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ParkingId { get; set; }

    public Guid VehicleId { get; set; }

    public string ReservationNumber { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public DateTimeOffset? ActualArrivalTime { get; set; }

    public DateTimeOffset? ActualDepartureTime { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}


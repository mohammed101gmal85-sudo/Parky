using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ReservationSpot;

public class ReservationSpotResponse
{
    public Guid Id { get; set; }

    public Guid ReservationId { get; set; }

    public Guid ParkingSpotId { get; set; }

    public decimal PricePerHour { get; set; }

    public decimal ReservedAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

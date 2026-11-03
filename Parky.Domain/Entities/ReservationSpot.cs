using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ReservationSpot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReservationId { get; set; }

    public Guid ParkingSpotId { get; set; }

    public decimal PricePerHour { get; set; }

    public decimal ReservedAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public Reservation Reservation { get; set; } = null!;

    public ParkingSpot ParkingSpot { get; set; } = null!;
}

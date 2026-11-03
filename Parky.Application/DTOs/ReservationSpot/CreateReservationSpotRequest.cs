using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ReservationSpot;

public class CreateReservationSpotRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    [Required]
    public Guid ParkingSpotId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal PricePerHour { get; set; }
}

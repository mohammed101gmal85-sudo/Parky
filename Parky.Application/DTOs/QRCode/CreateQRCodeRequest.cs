using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.QRCode;

public class CreateQRCodeRequest
{
    [Required]
    public Guid ReservationId { get; set; }

    [Range(1, 720)]
    public int ValidityMinutes { get; set; } = 60;
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.QRCode;

public class QRCodeResponse
{
    public Guid Id { get; set; }

    public Guid ReservationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public bool IsRevoked { get; set; }

    public int UsageCount { get; set; }
}


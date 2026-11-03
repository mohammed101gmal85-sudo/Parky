using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parky.Domain.Entities;

public class ParkingImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ParkingId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string? PublicId { get; set; }

    public string? AltText { get; set; }

    public bool IsPrimary { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public Parking Parking { get; set; } = null!;
}


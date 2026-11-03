using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.DTOs.ParkingImage;

public sealed class ParkingImageResponse
{
    public Guid Id { get; init; }

    public Guid ParkingId { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public string? PublicId { get; init; }

    public string? AltText { get; init; }

    public bool IsPrimary { get; init; }

    public int DisplayOrder { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}


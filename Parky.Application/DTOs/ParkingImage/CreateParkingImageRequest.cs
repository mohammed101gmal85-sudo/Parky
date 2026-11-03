using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
namespace Parky.Application.DTOs.ParkingImage;

public sealed class CreateParkingImageRequest
{
    public Guid ParkingId { get; init; }

    public Stream ImageContent { get; init; } = Stream.Null;

    public string ContentType { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public bool IsPrimary { get; init; }

    public int DisplayOrder { get; init; }
}


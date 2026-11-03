using Microsoft.AspNetCore.Http;

namespace Parky.API.Models.ParkingImage;

public sealed class UpdateParkingImageForm
{
    public Guid ParkingId { get; init; }

    public IFormFile? Image { get; init; }

    public string? AltText { get; init; }

    public bool IsPrimary { get; init; }

    public int DisplayOrder { get; init; }
}
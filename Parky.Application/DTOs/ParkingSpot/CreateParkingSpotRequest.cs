using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ParkingSpot;

public sealed class CreateParkingSpotRequest : IValidatableObject
{
    [Required]
    public Guid ParkingFloorId { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string SpotNumber { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal? XCoordinate { get; init; }

    [Range(typeof(decimal), "0", "1000000")]
    public decimal? YCoordinate { get; init; }

    [Range(typeof(decimal), "0.1", "1000000")]
    public decimal? Width { get; init; }

    [Range(typeof(decimal), "0.1", "1000000")]
    public decimal? Height { get; init; }

    public Guid? ParkingSpotTypeId { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(SpotNumber))
        {
            yield return new ValidationResult(
                "SpotNumber is required.",
                new[] { nameof(SpotNumber) });
        }

        if (XCoordinate.HasValue != YCoordinate.HasValue)
        {
            yield return new ValidationResult(
                "XCoordinate and YCoordinate must be provided together.",
                new[] { nameof(XCoordinate), nameof(YCoordinate) });
        }

        if (Width.HasValue != Height.HasValue)
        {
            yield return new ValidationResult(
                "Width and Height must be provided together.",
                new[] { nameof(Width), nameof(Height) });
        }
    }
}


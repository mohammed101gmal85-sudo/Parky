using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace Parky.Application.DTOs.ParkingOperatingHour;

public sealed class UpdateParkingOperatingHourStatusRequest : IValidatableObject
{
    public bool IsClosed { get; init; }

    public bool IsOpen24Hours { get; init; }

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (IsClosed && IsOpen24Hours)
        {
            yield return new ValidationResult(
                "A parking day cannot be closed and open 24 hours at the same time.",
                new[] { nameof(IsClosed), nameof(IsOpen24Hours) });
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingFloor;

namespace Parky.Application.Validation.ParkingFloor;

public sealed class CreateParkingFloorRequestValidator
    : AbstractValidator<CreateParkingFloorRequest>
{
    public CreateParkingFloorRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Floor name is required.")
            .Length(1, 100);

        RuleFor(x => x.FloorNumber)
            .InclusiveBetween(-10, 200)
            .WithMessage("Floor number must be between -10 and 200.");
    }
}
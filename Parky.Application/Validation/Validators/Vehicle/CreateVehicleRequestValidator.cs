using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Vehicle;

namespace Parky.Application.Validation.Validators.Vehicle;

public sealed class CreateVehicleRequestValidator
    : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.PlateNumber)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Plate number is required.")
            .Must(value => value.Trim().Length >= 2 &&
                           value.Trim().Length <= 20)
            .WithMessage("Plate number must be between 2 and 20 characters.");

        RuleFor(x => x.PlateCode)
            .MaximumLength(10)
            .WithMessage("Plate code cannot exceed 10 characters.");

        RuleFor(x => x.Make)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Vehicle make is required.")
            .Must(value => value.Trim().Length >= 2 &&
                           value.Trim().Length <= 50)
            .WithMessage("Vehicle make must be between 2 and 50 characters.");

        RuleFor(x => x.Model)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Vehicle model is required.")
            .Must(value => value.Trim().Length >= 1 &&
                           value.Trim().Length <= 50)
            .WithMessage("Vehicle model must be between 1 and 50 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(30)
            .WithMessage("Vehicle color cannot exceed 30 characters.");

        RuleFor(x => x.ManufacturingYear)
            .InclusiveBetween(1900, 2100)
            .When(x => x.ManufacturingYear.HasValue)
            .WithMessage("Manufacturing year must be between 1900 and 2100.");

        RuleFor(x => x.VehicleType)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Vehicle type is required.")
            .Must(value => value.Trim().Length >= 2 &&
                           value.Trim().Length <= 30)
            .WithMessage("Vehicle type must be between 2 and 30 characters.");
    }
}

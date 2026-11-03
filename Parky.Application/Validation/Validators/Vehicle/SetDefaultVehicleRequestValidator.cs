using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Vehicle;

namespace Parky.Application.Validation.Validators.Vehicle;

public sealed class SetDefaultVehicleRequestValidator
    : AbstractValidator<SetDefaultVehicleRequest>
{
    public SetDefaultVehicleRequestValidator()
    {
        RuleFor(x => x.VehicleId)
            .NotEmpty()
            .WithMessage("Vehicle ID is required.");
    }
}

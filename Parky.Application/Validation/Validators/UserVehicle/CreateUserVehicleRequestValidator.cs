using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.UserVehicle;

namespace Parky.Application.Validation.Validators.UserVehicle;

public sealed class CreateUserVehicleRequestValidator
    : AbstractValidator<CreateUserVehicleRequest>
{
    public CreateUserVehicleRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.VehicleId)
            .NotEmpty()
            .WithMessage("Vehicle ID is required.");
    }
}

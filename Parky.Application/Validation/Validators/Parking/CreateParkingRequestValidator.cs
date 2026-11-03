using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Parking;

namespace Parky.Application.Validation.Parking;

public sealed class CreateParkingRequestValidator
    : AbstractValidator<CreateParkingRequest>
{
    public CreateParkingRequestValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Parking name is required.")
            .Length(3, 150);

        RuleFor(x => x.Description)
            .NotNull()
            .MaximumLength(1000);

        RuleFor(x => x.Governorate)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Governorate is required.")
            .MaximumLength(100);

        RuleFor(x => x.City)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("City is required.")
            .MaximumLength(100);

        RuleFor(x => x.Address)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Address is required.")
            .MaximumLength(300);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);
    }
}
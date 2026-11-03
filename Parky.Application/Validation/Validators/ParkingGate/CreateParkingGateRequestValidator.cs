using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingGate;

namespace Parky.Application.Validation.ParkingGate;

public sealed class CreateParkingGateRequestValidator
    : AbstractValidator<CreateParkingGateRequest>
{
    public CreateParkingGateRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Gate name is required.")
            .Length(2, 100);

        RuleFor(x => x.GateCode)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Gate code is required.")
            .Length(2, 50);

        RuleFor(x => x.GateType)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Gate type is required.")
            .Length(2, 30);

        RuleFor(x => x.LocationDescription)
            .MaximumLength(250);

        RuleFor(x => x)
            .Must(x => x.IsEntryGate || x.IsExitGate)
            .WithMessage(
                "A gate must be configured as an entry gate, an exit gate, or both.");
    }
}
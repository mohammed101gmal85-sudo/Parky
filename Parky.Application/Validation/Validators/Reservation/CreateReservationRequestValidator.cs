using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Reservation;

namespace Parky.Application.Validation.Validators.Reservation;

public sealed class CreateReservationRequestValidator
    : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.ParkingId)
            .NotEmpty()
            .WithMessage("Parking ID is required.");

        RuleFor(x => x.VehicleId)
            .NotEmpty()
            .WithMessage("Vehicle ID is required.");

        RuleFor(x => x.StartTime)
            .Must(startTime => startTime > DateTimeOffset.UtcNow)
            .WithMessage("Reservation start time must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Reservation end time must be after its start time.");
    }
}

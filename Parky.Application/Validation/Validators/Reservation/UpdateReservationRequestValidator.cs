using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Reservation;

namespace Parky.Application.Validation.Validators.Reservation;

public sealed class UpdateReservationRequestValidator
    : AbstractValidator<UpdateReservationRequest>
{
    public UpdateReservationRequestValidator()
    {
        RuleFor(x => x.StartTime)
            .Must(startTime => startTime > DateTimeOffset.UtcNow)
            .WithMessage("Reservation start time must be in the future.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Reservation end time must be after its start time.");
    }
}


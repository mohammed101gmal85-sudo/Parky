using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ReservationSpot;

namespace Parky.Application.Validation.Validators.ReservationSpot;

public sealed class CreateReservationSpotRequestValidator
    : AbstractValidator<CreateReservationSpotRequest>
{
    public CreateReservationSpotRequestValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty()
            .WithMessage("Reservation ID is required.");

        RuleFor(x => x.ParkingSpotId)
            .NotEmpty()
            .WithMessage("Parking spot ID is required.");

        RuleFor(x => x.PricePerHour)
            .InclusiveBetween(0.01m, 1_000_000m)
            .WithMessage("Price per hour must be between 0.01 and 1,000,000.");
    }
}


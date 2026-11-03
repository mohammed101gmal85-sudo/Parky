using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ReservationSpot;

namespace Parky.Application.Validation.Validators.ReservationSpot;

public sealed class ReservationSpotQueryParametersValidator
    : AbstractValidator<ReservationSpotQueryParameters>
{
    public ReservationSpotQueryParametersValidator()
    {
        RuleFor(x => x.ReservationId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("Reservation ID is invalid.");

        RuleFor(x => x.ParkingSpotId)
            .Must(value => !value.HasValue || value.Value != Guid.Empty)
            .WithMessage("Parking spot ID is invalid.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.ReservationStatus)
            .MaximumLength(50)
            .When(x => x.ReservationStatus is not null)
            .WithMessage("Reservation status cannot exceed 50 characters.");

        RuleFor(x => x.MinPricePerHour)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Minimum price per hour cannot be negative.");

        RuleFor(x => x.MaxPricePerHour)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Maximum price per hour cannot be negative.");

        RuleFor(x => x.MinPricePerHour)
            .Must((parameters, minValue) =>
                !minValue.HasValue ||
                !parameters.MaxPricePerHour.HasValue ||
                minValue.Value <= parameters.MaxPricePerHour.Value)
            .WithMessage("Minimum price per hour cannot be greater than maximum price per hour.");

        RuleFor(x => x.MinReservedAmount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Minimum reserved amount cannot be negative.");

        RuleFor(x => x.MaxReservedAmount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Maximum reserved amount cannot be negative.");

        RuleFor(x => x.MinReservedAmount)
            .Must((parameters, minValue) =>
                !minValue.HasValue ||
                !parameters.MaxReservedAmount.HasValue ||
                minValue.Value <= parameters.MaxReservedAmount.Value)
            .WithMessage("Minimum reserved amount cannot be greater than maximum reserved amount.");

        RuleFor(x => x.CreatedFrom)
            .Must((parameters, createdFrom) =>
                !createdFrom.HasValue ||
                !parameters.CreatedTo.HasValue ||
                createdFrom.Value <= parameters.CreatedTo.Value)
            .WithMessage("Created from date cannot be later than created to date.");
    }
}

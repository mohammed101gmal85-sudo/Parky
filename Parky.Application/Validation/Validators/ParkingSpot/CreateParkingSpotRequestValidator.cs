using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingSpot;

namespace Parky.Application.Validation.ParkingSpot;

public sealed class CreateParkingSpotRequestValidator
    : AbstractValidator<CreateParkingSpotRequest>
{
    public CreateParkingSpotRequestValidator()
    {
        RuleFor(x => x.ParkingFloorId)
            .NotEmpty()
            .WithMessage("Parking floor ID is required.");

        RuleFor(x => x.SpotNumber)
            .Cascade(CascadeMode.Stop)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Spot number is required.")
            .Length(1, 50);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.XCoordinate)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0m && value.Value <= 1000000m))
            .WithMessage("XCoordinate must be between 0 and 1000000.");

        RuleFor(x => x.YCoordinate)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0m && value.Value <= 1000000m))
            .WithMessage("YCoordinate must be between 0 and 1000000.");

        RuleFor(x => x.Width)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0.1m && value.Value <= 1000000m))
            .WithMessage("Width must be between 0.1 and 1000000.");

        RuleFor(x => x.Height)
            .Must(value =>
                !value.HasValue ||
                (value.Value >= 0.1m && value.Value <= 1000000m))
            .WithMessage("Height must be between 0.1 and 1000000.");

        RuleFor(x => x)
            .Must(x =>
                x.XCoordinate.HasValue == x.YCoordinate.HasValue)
            .WithMessage(
                "XCoordinate and YCoordinate must be provided together.");

        RuleFor(x => x)
            .Must(x =>
                x.Width.HasValue == x.Height.HasValue)
            .WithMessage(
                "Width and Height must be provided together.");

        RuleFor(x => x.ParkingSpotTypeId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking spot type ID cannot be empty.");
    }
}

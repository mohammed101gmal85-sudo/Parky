using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingSpot;

namespace Parky.Application.Validation.ParkingSpot;

public sealed class UpdateParkingSpotStatusRequestValidator
    : AbstractValidator<UpdateParkingSpotStatusRequest>
{
    public UpdateParkingSpotStatusRequestValidator()
    {
        RuleFor(x => x)
            .Must(x =>
                x.IsAvailable.HasValue ||
                x.IsActive.HasValue)
            .WithMessage(
                "At least one status field must be provided.");

        RuleFor(x => x)
            .Must(x =>
                !(x.IsActive == false && x.IsAvailable == true))
            .WithMessage(
                "An inactive parking spot cannot be marked as available.");
    }
}
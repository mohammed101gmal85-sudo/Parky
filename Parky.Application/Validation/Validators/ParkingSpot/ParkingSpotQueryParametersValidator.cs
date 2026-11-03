using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingSpot;

namespace Parky.Application.Validation.ParkingSpot;

public sealed class ParkingSpotQueryParametersValidator
    : AbstractValidator<ParkingSpotQueryParameters>
{
    public ParkingSpotQueryParametersValidator()
    {
        RuleFor(x => x.ParkingFloorId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking floor ID cannot be empty.");

        RuleFor(x => x.ParkingSpotTypeId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking spot type ID cannot be empty.");

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}
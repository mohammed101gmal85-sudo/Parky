using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Parking;

namespace Parky.Application.Validation.Parking;

public sealed class ParkingQueryParametersValidator
    : AbstractValidator<ParkingQueryParameters>
{
    public ParkingQueryParametersValidator()
    {
        RuleFor(x => x.Governorate)
            .MaximumLength(100)
            .When(x => x.Governorate is not null);

        RuleFor(x => x.City)
            .MaximumLength(100)
            .When(x => x.City is not null);

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}
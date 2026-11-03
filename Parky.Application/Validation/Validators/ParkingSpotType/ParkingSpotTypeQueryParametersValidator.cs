using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingSpotType;

namespace Parky.Application.Validation.ParkingSpotType;

public sealed class ParkingSpotTypeQueryParametersValidator
    : AbstractValidator<ParkingSpotTypeQueryParameters>
{
    public ParkingSpotTypeQueryParametersValidator()
    {
        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("SearchTerm cannot exceed 200 characters.");

        RuleFor(x => x.MinAdditionalHourlyRate)
            .Must(rate => !rate.HasValue || rate.Value >= 0m)
            .WithMessage("Minimum additional hourly rate cannot be negative.");

        RuleFor(x => x.MaxAdditionalHourlyRate)
            .Must(rate => !rate.HasValue || rate.Value >= 0m)
            .WithMessage("Maximum additional hourly rate cannot be negative.");

        RuleFor(x => x.MaxAdditionalHourlyRate)
            .Must((parameters, maxRate) =>
                !parameters.MinAdditionalHourlyRate.HasValue ||
                !maxRate.HasValue ||
                parameters.MinAdditionalHourlyRate.Value <= maxRate.Value)
            .WithMessage(
                "MinAdditionalHourlyRate cannot be greater than MaxAdditionalHourlyRate.");

        RuleFor(x => x.CreatedTo)
            .Must((parameters, createdTo) =>
                !parameters.CreatedFrom.HasValue ||
                !createdTo.HasValue ||
                parameters.CreatedFrom.Value <= createdTo.Value)
            .WithMessage(
                "CreatedFrom cannot be later than CreatedTo.");

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}
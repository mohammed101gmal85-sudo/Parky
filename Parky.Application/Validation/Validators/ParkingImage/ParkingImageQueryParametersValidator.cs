using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingImage;

namespace Parky.Application.Validation.ParkingImage;

public sealed class ParkingImageQueryParametersValidator
    : AbstractValidator<ParkingImageQueryParameters>
{
    public ParkingImageQueryParametersValidator()
    {
        RuleFor(x => x.ParkingId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking ID is invalid.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("SearchTerm cannot exceed 200 characters.");

        RuleFor(x => x.MinDisplayOrder)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("MinDisplayOrder cannot be negative.");

        RuleFor(x => x.MaxDisplayOrder)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("MaxDisplayOrder cannot be negative.");

        RuleFor(x => x.MaxDisplayOrder)
            .Must((parameters, maxDisplayOrder) =>
                !parameters.MinDisplayOrder.HasValue ||
                !maxDisplayOrder.HasValue ||
                parameters.MinDisplayOrder.Value <= maxDisplayOrder.Value)
            .WithMessage(
                "MinDisplayOrder cannot be greater than MaxDisplayOrder.");

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
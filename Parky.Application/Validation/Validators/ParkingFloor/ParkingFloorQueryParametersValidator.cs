using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingFloor;

namespace Parky.Application.Validation.ParkingFloor;

public sealed class ParkingFloorQueryParametersValidator
    : AbstractValidator<ParkingFloorQueryParameters>
{
    public ParkingFloorQueryParametersValidator()
    {
        RuleFor(x => x.ParkingId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking ID cannot be empty.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.CreatedTo)
            .Must((parameters, createdTo) =>
                !parameters.CreatedFrom.HasValue ||
                !createdTo.HasValue ||
                createdTo.Value >= parameters.CreatedFrom.Value)
            .WithMessage("CreatedTo cannot be earlier than CreatedFrom.");

        RuleFor(x => x.UpdatedTo)
            .Must((parameters, updatedTo) =>
                !parameters.UpdatedFrom.HasValue ||
                !updatedTo.HasValue ||
                updatedTo.Value >= parameters.UpdatedFrom.Value)
            .WithMessage("UpdatedTo cannot be earlier than UpdatedFrom.");

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}
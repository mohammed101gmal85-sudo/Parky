using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.ParkingGate;

namespace Parky.Application.Validation.ParkingGate;

public sealed class ParkingGateQueryParametersValidator
    : AbstractValidator<ParkingGateQueryParameters>
{
    public ParkingGateQueryParametersValidator()
    {
        RuleFor(x => x.ParkingId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("Parking ID is invalid.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .WithMessage("SearchTerm cannot exceed 200 characters.");

        RuleFor(x => x.CreatedTo)
            .Must((parameters, createdTo) =>
                !parameters.CreatedFrom.HasValue ||
                !createdTo.HasValue ||
                createdTo.Value >= parameters.CreatedFrom.Value)
            .WithMessage(
                "CreatedFrom cannot be later than CreatedTo.");

        RuleFor(x => x.UpdatedTo)
            .Must((parameters, updatedTo) =>
                !parameters.UpdatedFrom.HasValue ||
                !updatedTo.HasValue ||
                updatedTo.Value >= parameters.UpdatedFrom.Value)
            .WithMessage(
                "UpdatedFrom cannot be later than UpdatedTo.");

        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");
    }
}
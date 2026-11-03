using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Vehicle;

namespace Parky.Application.Validation.Validators.Vehicle;

public sealed class VehicleQueryParametersValidator
    : AbstractValidator<VehicleQueryParameters>
{
    public VehicleQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.UserId)
            .Must(value =>
                !value.HasValue ||
                value.Value != Guid.Empty)
            .WithMessage("User ID is invalid.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.VehicleType)
            .MaximumLength(100)
            .When(x => x.VehicleType is not null)
            .WithMessage("Vehicle type cannot exceed 100 characters.");

        RuleFor(x => x.Make)
            .MaximumLength(100)
            .When(x => x.Make is not null)
            .WithMessage("Vehicle make cannot exceed 100 characters.");

        RuleFor(x => x.Model)
            .MaximumLength(100)
            .When(x => x.Model is not null)
            .WithMessage("Vehicle model cannot exceed 100 characters.");

        RuleFor(x => x.Color)
            .MaximumLength(50)
            .When(x => x.Color is not null)
            .WithMessage("Vehicle color cannot exceed 50 characters.");

        RuleFor(x => x.MinManufacturingYear)
            .Must(value =>
                !value.HasValue ||
                value.Value >= 1886)
            .WithMessage("Minimum manufacturing year is invalid.");

        RuleFor(x => x.MaxManufacturingYear)
            .Must(value =>
                !value.HasValue ||
                value.Value >= 1886)
            .WithMessage("Maximum manufacturing year is invalid.");

        RuleFor(x => x.MinManufacturingYear)
            .Must((parameters, minYear) =>
                !minYear.HasValue ||
                !parameters.MaxManufacturingYear.HasValue ||
                minYear.Value <= parameters.MaxManufacturingYear.Value)
            .WithMessage("Minimum manufacturing year cannot be greater than maximum manufacturing year.");

        RuleFor(x => x.CreatedFrom)
            .Must((parameters, createdFrom) =>
                !createdFrom.HasValue ||
                !parameters.CreatedTo.HasValue ||
                createdFrom.Value <= parameters.CreatedTo.Value)
            .WithMessage("Created from date cannot be later than created to date.");

        RuleFor(x => x.UpdatedFrom)
            .Must((parameters, updatedFrom) =>
                !updatedFrom.HasValue ||
                !parameters.UpdatedTo.HasValue ||
                updatedFrom.Value <= parameters.UpdatedTo.Value)
            .WithMessage("Updated from date cannot be later than updated to date.");
    }
}

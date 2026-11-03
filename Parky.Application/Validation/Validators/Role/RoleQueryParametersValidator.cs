using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Role;

namespace Parky.Application.Validation.Validators.Role;

public sealed class RoleQueryParametersValidator
    : AbstractValidator<RoleQueryParameters>
{
    public RoleQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.MinUsersCount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Minimum users count cannot be negative.");

        RuleFor(x => x.MaxUsersCount)
            .Must(value => !value.HasValue || value.Value >= 0)
            .WithMessage("Maximum users count cannot be negative.");

        RuleFor(x => x.MinUsersCount)
            .Must((parameters, minValue) =>
                !minValue.HasValue ||
                !parameters.MaxUsersCount.HasValue ||
                minValue.Value <= parameters.MaxUsersCount.Value)
            .WithMessage("Minimum users count cannot be greater than maximum users count.");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Role;

namespace Parky.Application.Validation.Validators.Role;

public sealed class RoleUserQueryParametersValidator
    : AbstractValidator<RoleUserQueryParameters>
{
    public RoleUserQueryParametersValidator()
    {
        RuleFor(x => x.PageNumber)
            .Must((parameters, pageNumber) =>
                (long)(pageNumber - 1) * parameters.PageSize <= int.MaxValue)
            .WithMessage("The requested page number is too large.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(200)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.CreatedFrom)
            .Must((parameters, createdFrom) =>
                !createdFrom.HasValue ||
                !parameters.CreatedTo.HasValue ||
                createdFrom.Value <= parameters.CreatedTo.Value)
            .WithMessage("Created from date cannot be later than created to date.");
    }
}

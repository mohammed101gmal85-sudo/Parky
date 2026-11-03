using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Role;

namespace Parky.Application.Validation.Validators.Role;

public sealed class RemoveUserFromRoleRequestValidator
    : AbstractValidator<RemoveUserFromRoleRequest>
{
    public RemoveUserFromRoleRequestValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithMessage("Role ID is required.");
    }
}

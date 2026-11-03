using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Role;

namespace Parky.Application.Validation.Validators.Role;

public sealed class CreateRoleRequestValidator
    : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.RoleName)
            .Cascade(CascadeMode.Stop)
            .Must(roleName => !string.IsNullOrWhiteSpace(roleName))
            .WithMessage("Role name is required.")
            .Must(roleName =>
                roleName.Trim().Length <= 256)
            .WithMessage("Role name cannot exceed 256 characters.");
    }
}


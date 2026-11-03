using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.Auth;

namespace Parky.Application.Validation.Auth;

public sealed class RefreshTokenRequestValidator
    : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .Must(x => !string.IsNullOrWhiteSpace(x))
            .WithMessage("Refresh token is required.");
    }
}
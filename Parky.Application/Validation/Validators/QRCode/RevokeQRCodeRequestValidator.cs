using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Parky.Application.DTOs.QRCode;

namespace Parky.Application.Validation.Validators.QRCode;

public sealed class RevokeQRCodeRequestValidator
    : AbstractValidator<RevokeQRCodeRequest>
{
    public RevokeQRCodeRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .Must(reason => !string.IsNullOrWhiteSpace(reason))
            .WithMessage("A revocation reason is required.")
            .MaximumLength(200)
            .WithMessage("Revocation reason cannot exceed 200 characters.");
    }
}